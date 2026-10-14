using Dalamud.Bindings.ImGui;
using Dalamud.Interface.Colors;
using Dalamud.Interface.Utility;
using Dalamud.Interface.Utility.Raii;
using Dalamud.Interface.Windowing;
using System;
using System.Numerics;
using System.Threading.Tasks;

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
        : base($"XIVRus v{Plugin.PluginInterface.Manifest.AssemblyVersion}###XIVMain")
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

        using (var header = ImRaii.Header("System Status", ImGuiTreeNodeFlags.DefaultOpen))
        {
            if (header.Success)
            {
                ImGui.BulletText($"Penumbra: {(state.PenumbraEnabled ? "Enabled" : "Disabled")}");

                ImGui.Separator();

                ImGui.BulletText($"Translation Status: {(state.Translation.Installed ? "Installed" : "Not Installed")}");
                ImGui.BulletText($"Translation Version: {state.Translation.Version ?? "Unknown"}");
                ImGui.BulletText($"Translation Remote Version: {state.Translation.Version ?? "Unknown"}");

                ImGui.Separator();

                ImGui.BulletText($"Penumbra Mod Status: {(state.Penumbra.Installed ? "Installed" : "Not Installed")}");
                ImGui.BulletText($"Penumbra Mod Version: {state.Penumbra.Version ?? "Unknown"}");
                ImGui.BulletText($"Penumbra Remote Version: {state.Penumbra.Version ?? "Unknown"}");

                ImGui.Separator();

                ImGui.Text("Last Check: ");
                ImGui.SameLine();
                ImGui.TextDisabled(_plugin.Configuration.LastUpdateCheck == default ? "Never" : _plugin.Configuration.LastUpdateCheck.ToString("G"));
            }
        }

        if (!string.IsNullOrEmpty(Plugin.State.LastChangelog))
        {
            using (var header = ImRaii.Header("Last Changelog", ImGuiTreeNodeFlags.None))
            {
                if (header.Success)
                {
                    ImGui.TextWrapped(Plugin.State.LastChangelog);
                }
            }
        }

        using (var header = ImRaii.Header("Actions", ImGuiTreeNodeFlags.DefaultOpen))
        {
            if (header.Success)
            {
                if (ImGui.Button("Refresh", new Vector2(-1, 0)))
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
                    if (ImGui.Button("Update components", new Vector2(-1, 0)))
                    {
                        _downloadTask ??= Plugin.networkService.DownloadLatestModAsync();
                    }
                }

                if (_downloadTask?.IsCompleted == true)
                {
                    _downloadTask = null;
                }

                if (ImGui.Button("Open config", new Vector2(-1, 0)))
                {
                    _plugin.ToggleConfigUi();
                }
            }
        }

        using (var header = ImRaii.Header("Diagnostics", ImGuiTreeNodeFlags.None))
        {
            if (header.Success)
            {
                ImGui.TextDisabled("Branch: ");
                ImGui.SameLine();
                ImGui.Text(_plugin.Configuration.Channel.ToString());

                ImGui.TextDisabled("Tester Access Allowance: ");
                ImGui.SameLine();

                if (!_plugin.Configuration.TesterHumanCheck)
                    ImGui.TextColored(ImGuiColors.DalamudYellow, "Not Allowed");
                else
                    ImGui.TextColored(ImGuiColors.HealerGreen, "Allowed");
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
                text = "Engine is up to date";
                break;

            case OverallStatus.UpdateAvailable:
                color = ImGuiColors.DalamudYellow;
                text = "Update available";
                break;

            case OverallStatus.Disabled:
                color = ImGuiColors.DalamudRed;
                text = "Engine temporarily disabled";
                break;

            default:
                color = ImGuiColors.DalamudRed;
                text = "Unable to determine status";
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
