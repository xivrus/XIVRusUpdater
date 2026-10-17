using Dalamud.Bindings.ImGui;
using Dalamud.Interface.Colors;
using Dalamud.Interface.Components;
using Dalamud.Interface.Utility;
using Dalamud.Interface.Utility.Raii;
using Dalamud.Interface.Windowing;
using System;
using System.Numerics;
using XIVRusUpdater.Core.Components;
using XIVRusUpdater.Resource;

namespace XIVRusUpdater.Windows;

public class ConfigWindow : Window, IDisposable
{
    private readonly Configuration _config;

    public ConfigWindow(Plugin plugin) : base($"{Localization.TranslationConfiguration}###XIVConfig")
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
        using(var header = ImRaii.Header(Localization.General, ImGuiTreeNodeFlags.DefaultOpen))
        {
            if(header.Success)
            {
                using (var child = ImRaii.Child("GeneralSettings", new Vector2(0, 120) * ImGuiHelpers.GlobalScale, true))
                {
                    if (child.Success)
                    {
                        var currentEngine = TranslationEngines.Get(_config.EngineId);

                        using (var combo = ImRaii.Combo(Localization.TranslationEngine, currentEngine?.DisplayName ?? Localization.Unknown))
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

                        if (ImGui.Checkbox(Localization.ShowNotifications, ref showNotify))
                        {
                            _config.ShowNotifications = showNotify;
                            _config.Save();
                        }
                        if (ImGui.Checkbox(Localization.ShowChangelogAfterUpdate, ref showChangelog))
                        {
                            _config.ShowChangelogAfterUpdate = showChangelog;
                            _config.Save();
                        }
                    }
                }
            }
        }

        using(var header = ImRaii.Header(Localization.Components, ImGuiTreeNodeFlags.DefaultOpen))
        {
            if (header.Success)
            {
                var translationFilter = Plugin.filter;
                
                using(ImRaii.PushColor(ImGuiCol.Text, ImGuiColors.DalamudYellow))
                {
                    ImGui.TextWrapped(Localization.RebootImpotance);
                }

                if (ImGui.Button(Localization.EnableAll))
                {
                    _config.DisabledComponents.Clear();
                    translationFilter.Rebuild(_config.DisabledComponents);
                }

                ImGui.SameLine();

                if (ImGui.Button(Localization.DisableAll))
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

                _config.Save();
            }
        }

        using(var header = ImRaii.Header(Localization.UpdateSection, ImGuiTreeNodeFlags.None))
        {
            if (header.Success)
            {
                using (var child = ImRaii.Child("UpdateSettings", new Vector2(0, 110) * ImGuiHelpers.GlobalScale, true))
                {
                    if (child.Success)
                    {
                        int interval = _config.UpdateCheckIntervalMinutes;
                        if (ImGui.SliderInt(Localization.CheckIntervalMinutes, ref interval, 5, 1440))
                        {
                            _config.UpdateCheckIntervalMinutes = interval;
                            _config.Save();
                        }

                        ImGui.Spacing();

                        var autoDownload = _config.AutoDownloadUpdates;
                        
                        if (ImGui.Checkbox(Localization.AutoDownloadUpdates, ref autoDownload))
                        {
                            _config.AutoDownloadUpdates = autoDownload;
                            _config.Save();
                        }
                    }
                }
            }
        }

        using(var header = ImRaii.Header(Localization.AccessLevel, ImGuiTreeNodeFlags.None))
        {
            if (header.Success)
            {
                using (var child = ImRaii.Child("TesterSettings", new Vector2(0, 100) * ImGuiHelpers.GlobalScale, true))
                {
                    if (child.Success)
                    {
                        using (var combo = ImRaii.Combo(Localization.Channel, _config.Channel.ToString()))
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

                        ImGui.TextWrapped(Localization.TestVersionWarning);

                        if (ImGui.Checkbox(Localization.AcknowledgeTestVersionRisk, ref testerHumanCheck))
                        {
                            _config.TesterHumanCheck = testerHumanCheck;
                            _config.Save();
                        }
                    }
                }
            }
        }

        using(var header = ImRaii.Header(Localization.Information, ImGuiTreeNodeFlags.None))
        {
            if (header.Success)
            {
                using (var child = ImRaii.Child("InformationPanel", new Vector2(0, 140) * ImGuiHelpers.GlobalScale, true))
                {
                    if (child.Success)
                    {
                        ImGui.Text(Localization.LastInstalledTranslation);
                        ImGui.SameLine();
                        ImGui.TextDisabled(_config.LastInstalledVersion);

                        ImGui.Text(Localization.LastRemoteTranslation);
                        ImGui.SameLine();
                        ImGui.TextDisabled(_config.LastKnownRemoteVersion);

                        ImGui.Text(Localization.LastInstalledPenumbraMod);
                        ImGui.SameLine();
                        ImGui.TextDisabled(_config.LastInstalledPenumbra);

                        ImGui.Text(Localization.LastRemotePenumbraMod);
                        ImGui.SameLine();
                        ImGui.TextDisabled(_config.LastKnownRemotePenumbra);

                        ImGui.Text(Localization.LastUpdateCheck);
                        ImGui.SameLine();
                        ImGui.TextDisabled(_config.LastUpdateCheck.ToString("g"));

                        ImGui.Text(Localization.LastSuccessfulUpdate);
                        ImGui.SameLine();
                        ImGui.TextDisabled(_config.LastSuccessfulUpdate.ToString("g"));
                    }
                }
            }
        }
    }
}
