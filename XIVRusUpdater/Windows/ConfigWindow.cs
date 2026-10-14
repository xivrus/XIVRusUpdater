using Dalamud.Bindings.ImGui;
using Dalamud.Interface.Components;
using Dalamud.Interface.Utility;
using Dalamud.Interface.Utility.Raii;
using Dalamud.Interface.Windowing;
using System;
using System.Numerics;
using XIVRusUpdater.Core.Components;

namespace XIVRusUpdater.Windows;

public class ConfigWindow : Window, IDisposable
{
    private readonly Configuration _config;

    public ConfigWindow(Plugin plugin) : base("Translation configuration###XIVConfig")
    {
        _config = plugin.Configuration;

        SizeConstraints = new WindowSizeConstraints
        {
            MinimumSize = new Vector2(375, 330) * ImGuiHelpers.GlobalScale,
            MaximumSize = new Vector2(float.MaxValue, float.MaxValue)
        };

    }

    public void Dispose() { }

    public override void Draw()
    {
        using(var header = ImRaii.Header("General", ImGuiTreeNodeFlags.DefaultOpen))
        {
            if(header.Success)
            {
                using (var child = ImRaii.Child("GeneralSettings", new Vector2(0, 120) * ImGuiHelpers.GlobalScale, true))
                {
                    if (child.Success)
                    {
                        var currentEngine = TranslationEngines.Get(_config.EngineId);

                        using (var combo = ImRaii.Combo("Translation Engine", currentEngine?.DisplayName ?? "Unknown"))
                        {
                            if (combo.Success)
                            {
                                foreach (var engine in TranslationEngines.All)
                                {
                                    bool selected = engine.Id == _config.EngineId;

                                    if (ImGui.Selectable(engine.DisplayName, selected))
                                    {
                                        _config.EngineId = engine.Id;
                                        _config.Save();
                                    }

                                    if (selected)
                                        ImGui.SetItemDefaultFocus();
                                }
                            }
                        }

                        var showNotify = _config.ShowNotifications;
                        var showChangelog = _config.ShowChangelogAfterUpdate;

                        if (ImGui.Checkbox("Show notifications", ref showNotify))
                        {
                            _config.ShowNotifications = showNotify;
                            _config.Save();
                        }
                        if (ImGui.Checkbox("Show changelog after update", ref showChangelog))
                        {
                            _config.ShowChangelogAfterUpdate = showChangelog;
                            _config.Save();
                        }
                    }
                }
            }
        }

        using(var header = ImRaii.Header("Components", ImGuiTreeNodeFlags.DefaultOpen))
        {
            if (header.Success)
            {
                var translationFilter = Plugin.filter;

                if (ImGui.Button("Enable All"))
                {
                    _config.DisabledComponents.Clear();
                    translationFilter.Rebuild(_config.DisabledComponents);
                }

                ImGui.SameLine();

                if (ImGui.Button("Disable All"))
                {
                    _config.DisabledComponents.Clear();

                    foreach (var component in TranslationComponents.All)
                        _config.DisabledComponents.Add(component.Id);

                    translationFilter.Rebuild(_config.DisabledComponents);
                }

                ImGui.Separator();

                foreach (var component in TranslationComponents.All)
                {
                    bool enabled = !_config.DisabledComponents.Contains(component.Id);

                    if (ImGui.Checkbox(component.DisplayName, ref enabled))
                    {
                        if (enabled)
                            _config.DisabledComponents.Remove(component.Id);
                        else
                            _config.DisabledComponents.Add(component.Id);

                        translationFilter.Rebuild(_config.DisabledComponents);
                    }

                    ImGui.SameLine();

                    ImGuiComponents.HelpMarker(component.Description);
                }
            }
        }

        using(var header = ImRaii.Header("Update", ImGuiTreeNodeFlags.None))
        {
            if (header.Success)
            {
                using (var child = ImRaii.Child("UpdateSettings", new Vector2(0, 110) * ImGuiHelpers.GlobalScale, true))
                {
                    if (child.Success)
                    {
                        int interval = _config.UpdateCheckIntervalMinutes;
                        if (ImGui.SliderInt("Check interval (minutes)", ref interval, 5, 1440))
                        {
                            _config.UpdateCheckIntervalMinutes = interval;
                            _config.Save();
                        }

                        ImGui.Spacing();

                        var autoDownload = _config.AutoDownloadUpdates;
                        var autoInstall = _config.AutoInstallUpdates;

                        if (ImGui.Checkbox("Auto download updates", ref autoDownload))
                        {
                            _config.AutoDownloadUpdates = autoDownload;
                            _config.Save();
                        }
                        if (ImGui.Checkbox("Auto install updates", ref autoInstall))
                        {
                            _config.AutoInstallUpdates = autoInstall;
                            _config.Save();
                        }
                    }
                }
            }
        }

        using(var header = ImRaii.Header("Access Level", ImGuiTreeNodeFlags.None))
        {
            if (header.Success)
            {
                using (var child = ImRaii.Child("TesterSettings", new Vector2(0, 100) * ImGuiHelpers.GlobalScale, true))
                {
                    if (child.Success)
                    {
                        using (var combo = ImRaii.Combo("Channel", _config.Channel.ToString()))
                        {
                            if (combo.Success)
                            {
                                foreach (var channel in Enum.GetValues<UpdateChannel>())
                                {
                                    bool isTestChannel = channel != UpdateChannel.Stable;

                                    if (isTestChannel && !_config.TesterHumanCheck)
                                        continue;

                                    bool selected = channel == _config.Channel;

                                    if (ImGui.Selectable(channel.ToString(), selected))
                                    {
                                        _config.Channel = channel;
                                        _config.Save();
                                    }

                                    if (selected) ImGui.SetItemDefaultFocus();
                                }
                            }
                        }

                        ImGui.Separator();

                        bool testerHumanCheck = _config.TesterHumanCheck;

                        ImGui.TextWrapped("Test versions may contain unverified translations, incomplete changes, and unexpected issues. The game or localization may behave incorrectly.");

                        if (ImGui.Checkbox("I understand the risks of using test versions.", ref testerHumanCheck))
                        {
                            _config.TesterHumanCheck = testerHumanCheck;
                            _config.Save();
                        }
                    }
                }
            }
        }

        using(var header = ImRaii.Header("Information", ImGuiTreeNodeFlags.None))
        {
            if (header.Success)
            {
                using (var child = ImRaii.Child("InformationPanel", new Vector2(0, 140) * ImGuiHelpers.GlobalScale, true))
                {
                    if (child.Success)
                    {
                        ImGui.Text("Last installed translation: ");
                        ImGui.SameLine();
                        ImGui.TextDisabled(_config.LastInstalledVersion);

                        ImGui.Text("Last remote translation: ");
                        ImGui.SameLine();
                        ImGui.TextDisabled(_config.LastKnownRemoteVersion);

                        ImGui.Text("Last installed Penumbra mod: ");
                        ImGui.SameLine();
                        ImGui.TextDisabled(_config.LastInstalledPenumbra);

                        ImGui.Text("Last remote Penumbra mod: ");
                        ImGui.SameLine();
                        ImGui.TextDisabled(_config.LastKnownRemotePenumbra);

                        ImGui.Text("Last Update Check: ");
                        ImGui.SameLine();
                        ImGui.TextDisabled(_config.LastUpdateCheck.ToString("g"));

                        ImGui.Text("Last Successful Update: ");
                        ImGui.SameLine();
                        ImGui.TextDisabled(_config.LastSuccessfulUpdate.ToString("g"));
                    }
                }
            }
        }
    }
}
