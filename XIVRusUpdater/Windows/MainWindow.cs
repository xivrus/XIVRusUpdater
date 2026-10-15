using Dalamud.Bindings.ImGui;
using Dalamud.Interface.Colors;
using Dalamud.Interface.Utility;
using Dalamud.Interface.Utility.Raii;
using Dalamud.Interface.Windowing;
using System;
using System.Numerics;
using System.Threading.Tasks;
using XIVRusUpdater.Resource;

namespace XIVRusUpdater.Windows;

public class MainWindow : Window, IDisposable
{
    private readonly Plugin _plugin;
    private Task? _refreshTask;
    private Task? _downloadTask;
    
    private enum OverallStatus
    {
        Ok,
        UpdateAvailable,
        Disabled,
        Error
    }

    public MainWindow(Plugin plugin)
        : base($"{string.Format(Localization.PluginTitle, Plugin.PluginInterface.Manifest.AssemblyVersion)}###XIVMain")
    {
        _plugin = plugin;

        SizeConstraints = new WindowSizeConstraints
        {
            MinimumSize = new Vector2(375, 330),
            MaximumSize = new Vector2(float.MaxValue, float.MaxValue)
        };
    }

    public void Dispose() { }

    public override void Draw()
    {
        var state = Plugin.State;

        DrawStatusBanner();

        ImGui.Spacing();

        using (var header = ImRaii.Header(Localization.SystemStatus, ImGuiTreeNodeFlags.DefaultOpen))
        {
            if (header.Success)
            {
                ImGui.BulletText($"Penumbra: {(state.PenumbraEnabled ? Localization.PenumbraEnabled : Localization.PenumbraDisabled)}");

                ImGui.Separator();

                ImGui.BulletText(string.Format(Localization.TranslationStatus, state.Translation.Installed ? Localization.Installed : Localization.NotInstalled));
                ImGui.BulletText(string.Format(Localization.TranslationVersion, state.Translation.Version ?? Localization.Unknown));
                ImGui.BulletText(string.Format(Localization.RemoteTranslationVersion, state.Translation.RemoteVersion ?? Localization.Unknown));

                ImGui.Separator();

                ImGui.BulletText(string.Format(Localization.PenumbraModStatus, state.Penumbra.Installed ? Localization.Installed : Localization.NotInstalled));
                ImGui.BulletText(string.Format(Localization.PenumbraModVersion, state.Penumbra.Version ?? Localization.Unknown));
                ImGui.BulletText(string.Format(Localization.PenumbraModRemoteVersion, state.Penumbra.RemoteVersion ?? Localization.Unknown));

                ImGui.Separator();

                ImGui.Text(Localization.LastCheck);
                ImGui.SameLine();
                ImGui.TextDisabled(_plugin.Configuration.LastUpdateCheck == default ? Localization.Never : _plugin.Configuration.LastUpdateCheck.ToString("G"));
            }
        }

        if (!string.IsNullOrEmpty(Plugin.State.LastChangelog))
        {
            using (var header = ImRaii.Header(Localization.LastChangelog, ImGuiTreeNodeFlags.None))
            {
                if (header.Success)
                {
                    ImGui.TextWrapped(Plugin.State.LastChangelog);
                }
            }
        }

        using (var header = ImRaii.Header(Localization.Actions, ImGuiTreeNodeFlags.DefaultOpen))
        {
            if (header.Success)
            {
                if (ImGui.Button(Localization.Refresh, new Vector2(-1, 0)))
                {
                    _refreshTask ??= Plugin.networkService.CheckForUpdates();
                }

                if (_refreshTask?.IsCompleted == true)
                {
                    _refreshTask = null;
                }

                bool updateAvailable = Plugin.State.UpdateAvailable;

                using (var disable = ImRaii.Disabled(!updateAvailable))
                {
                    if (ImGui.Button(Localization.UpdateComponents, new Vector2(-1, 0)))
                    {
                        _downloadTask ??= Plugin.networkService.DownloadLatestModAsync();
                    }
                }

                if (_downloadTask?.IsCompleted == true)
                {
                    _downloadTask = null;
                }

                if (ImGui.Button(Localization.OpenConfiguration, new Vector2(-1, 0)))
                {
                    _plugin.ToggleConfigUi();
                }
            }
        }

        using (var header = ImRaii.Header(Localization.Diagnostics, ImGuiTreeNodeFlags.None))
        {
            if (header.Success)
            {
                ImGui.TextDisabled(Localization.Branch);
                ImGui.SameLine();
                ImGui.Text(_plugin.Configuration.Channel.ToString());

                ImGui.TextDisabled(Localization.TesterAccessAllowance);
                ImGui.SameLine();

                if (!_plugin.Configuration.TesterHumanCheck)
                    ImGui.TextColored(ImGuiColors.DalamudYellow, Localization.NotAllowed);
                else
                    ImGui.TextColored(ImGuiColors.HealerGreen, Localization.Allowed);
            }
        }
    }

    private OverallStatus GetOverallStatus()
    {
        var state = Plugin.State;

        if (!state.PenumbraEnabled)
            return OverallStatus.Error;

        if (Plugin.State.UpdateAvailable)
            return OverallStatus.UpdateAvailable;

        return OverallStatus.Ok;
    }

    private void DrawStatusBanner()
    {
        var status = GetOverallStatus();
        
        Vector4 color;
        string text;

        switch (status)
        {
            case OverallStatus.Ok:
                color = ImGuiColors.HealerGreen;
                text = Localization.EngineUpToDate;
                break;

            case OverallStatus.UpdateAvailable:
                color = ImGuiColors.DalamudYellow;
                text = Localization.UpdateAvailable;
                break;

            case OverallStatus.Disabled:
                color = ImGuiColors.DalamudRed;
                text = Localization.EngineTemporarilyDisabled;
                break;

            default:
                color = ImGuiColors.DalamudRed;
                text = Localization.StatusUndetermined;
                break;
        }

        using (ImRaii.PushColor(ImGuiCol.ChildBg, color * new Vector4(1, 1, 1, 0.15f)))
        {
            using (var child = ImRaii.Child("StatusBanner", new Vector2(-1, 50 * ImGuiHelpers.GlobalScale), true))
            {
                if (child.Success)
                {
                    using (ImRaii.PushColor(ImGuiCol.Text, color))
                    {
                        ImGui.SetCursorPosY(15);
                        ImGui.TextUnformatted(text);
                    }
                }
            }
        }
    }
}
