using Dalamud.Bindings.ImGui;
using Dalamud.Interface.Components;
using Dalamud.Interface.Utility.Raii;
using Dalamud.Interface.Windowing;
using System;
using System.Numerics;
using XIVRusUpdater.Core;
using XIVRusUpdater.Core.Components;
using XIVRusUpdater.Utils;

namespace XIVRusUpdater.Windows;

public class ConfigWindow : Window, IDisposable
{
    private readonly Configuration configuration;

    public ConfigWindow(Plugin plugin) : base("XIV Rus Config###XIVConfig")
    {
        SizeConstraints = new WindowSizeConstraints
        {
            MinimumSize = new Vector2(375, 330),
            MaximumSize = new Vector2(float.MaxValue, float.MaxValue)
        };

        configuration = plugin.Configuration;
    }

    public void Dispose() { }

    public override void Draw()
    {
        if (ImGui.CollapsingHeader("General", ImGuiTreeNodeFlags.DefaultOpen))
        {
            ImGui.BeginChild("GeneralSettings", new Vector2(0, 70), true);

            var currentEngine = TranslationEngines.Get(configuration.EngineId);

            if (ImGui.BeginCombo("Translation Engine", currentEngine?.DisplayName ?? "Unknown"))
            {
                foreach (var engine in TranslationEngines.All)
                {
                    bool selected = engine.Id == configuration.EngineId;

                    if (ImGui.Selectable(engine.DisplayName, selected))
                    {
                        configuration.EngineId = engine.Id;
                        configuration.Save();
                    }

                    if (selected)
                        ImGui.SetItemDefaultFocus();
                }

                ImGui.EndCombo();
            }

            var showNotify = configuration.ShowNotifications;
            var showChangelog = configuration.ShowChangelogAfterUpdate;

            if (ImGui.Checkbox("Show notifications", ref showNotify))
            {
                configuration.ShowNotifications = showNotify;
                configuration.Save();
            }
            if (ImGui.Checkbox("Show changelog after update", ref showChangelog))
            {
                configuration.ShowChangelogAfterUpdate = showChangelog;
                configuration.Save();
            }

            ImGui.EndChild();
        }

        if (ImGui.CollapsingHeader("Components"))
        {
            var translationFilter = Plugin.filter;

            if (ImGui.Button("Enable All"))
            {
                configuration.DisabledComponents.Clear();
                translationFilter.Rebuild(configuration.DisabledComponents);
            }

            ImGui.SameLine();

            if (ImGui.Button("Disable All"))
            {
                configuration.DisabledComponents.Clear();

                foreach (var component in TranslationComponents.All)
                    configuration.DisabledComponents.Add(component.Id);

                translationFilter.Rebuild(configuration.DisabledComponents);
            }

            ImGui.Separator();

            foreach (var component in TranslationComponents.All)
            {
                bool enabled = !configuration.DisabledComponents.Contains(component.Id);

                if (ImGui.Checkbox(component.DisplayName, ref enabled))
                {
                    if (enabled)
                        configuration.DisabledComponents.Remove(component.Id);
                    else
                        configuration.DisabledComponents.Add(component.Id);

                    translationFilter.Rebuild(configuration.DisabledComponents);
                }

                ImGui.SameLine();

                ImGuiComponents.HelpMarker(component.Description);
            }
        }

        if (ImGui.CollapsingHeader("Updates", ImGuiTreeNodeFlags.DefaultOpen))
        {
            ImGui.BeginChild("UpdateSettings", new Vector2(0, 180), true);

            ImGui.Spacing();

            int interval = configuration.UpdateCheckIntervalMinutes;
            if(ImGui.SliderInt("Check interval (minutes)", ref interval, 5, 1440))
            {
                configuration.UpdateCheckIntervalMinutes = interval;
                configuration.Save();
            }

            ImGui.Spacing();

            var autoDownload = configuration.AutoDownloadUpdates;
            var autoInstall = configuration.AutoInstallUpdates;
            
            if(ImGui.Checkbox("Auto download updates", ref autoDownload))
            {
                configuration.AutoDownloadUpdates = autoDownload;
                configuration.Save();
            }
            if(ImGui.Checkbox("Auto install updates", ref autoInstall))
            {
                configuration.AutoInstallUpdates = autoInstall;
                configuration.Save();
            }

            ImGui.EndChild();
        }

        if (ImGui.CollapsingHeader("Tester Access"))
        {
            ImGui.BeginChild("TesterSettings", new Vector2(0, 120), true);

            if (ImGui.BeginCombo("Channel", configuration.Channel.ToString()))
            {
                foreach (var channel in Enum.GetValues<UpdateChannel>())
                {
                    bool isTestChannel = channel != UpdateChannel.Stable;

                    if (isTestChannel && !configuration.TesterHumanCheck)
                        continue;

                    bool selected = channel == configuration.Channel;

                    if (ImGui.Selectable(channel.ToString(), selected))
                    {
                        configuration.Channel = channel;
                        configuration.Save();
                    }

                    if (selected) ImGui.SetItemDefaultFocus();
                }

                ImGui.EndCombo();
            }

            ImGui.Separator();

            bool testerHumanCheck = configuration.TesterHumanCheck;

            ImGui.TextWrapped("Test versions may contain unverified translations, incomplete changes, and unexpected issues. The game or localization may behave incorrectly.");

            if (ImGui.Checkbox("I understand the risks of using test versions.", ref testerHumanCheck))
            {
                configuration.TesterHumanCheck = testerHumanCheck;
                configuration.Save();
            }

            ImGui.EndChild();
        }

        if (ImGui.CollapsingHeader("Information"))
        {
            ImGui.BeginChild("InformationPanel", new Vector2(0, 140), true);

            ImGui.Text("Last installed translation: ");
            ImGui.SameLine();
            ImGui.TextDisabled(configuration.LastInstalledVersion);

            ImGui.Text("Last remote translation: ");
            ImGui.SameLine();
            ImGui.TextDisabled(configuration.LastKnownRemoteVersion);

            ImGui.Text("Last installed Penumbra mod: ");
            ImGui.SameLine();
            ImGui.TextDisabled(configuration.LastInstalledPenumbra);

            ImGui.Text("Last remote Penumbra mod: ");
            ImGui.SameLine();
            ImGui.TextDisabled(configuration.LastKnownRemotePenumbra);

            ImGui.Text("Last Update Check: ");
            ImGui.SameLine();
            ImGui.TextDisabled(configuration.LastUpdateCheck.ToString("g"));

            ImGui.Text("Last Successful Update: ");
            ImGui.SameLine();
            ImGui.TextDisabled(configuration.LastSuccessfulUpdate.ToString("g"));

            ImGui.EndChild();
        }
    }
}
