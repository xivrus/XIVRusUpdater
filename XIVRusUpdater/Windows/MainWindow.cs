using Dalamud.Bindings.ImGui;
using Dalamud.Interface.Colors;
using Dalamud.Interface.Utility.Raii;
using Dalamud.Interface.Windowing;
using System;
using System.Collections.Generic;
using System.Numerics;
using System.Threading.Tasks;
using XIVRusUpdater.Utils;

namespace XIVRusUpdater.Windows;

public class MainWindow : Window, IDisposable
{
    private readonly string LogoImagePath;
    private readonly Plugin plugin;
    private Task? refreshTask;
    private Task? downloadTask;
    
    private enum OverallStatus
    {
        Ok,
        UpdateAvailable,
        Warning,
        Disabled,
        Error
    }

    public MainWindow(Plugin plugin, string logoImagePath)
        : base("XIV Rus Auto Updater###XIVMain")
    {
        SizeConstraints = new WindowSizeConstraints
        {
            MinimumSize = new Vector2(375, 330),
            MaximumSize = new Vector2(float.MaxValue, float.MaxValue)
        };

        LogoImagePath = logoImagePath;
        this.plugin = plugin;
    }

    public void Dispose() { }

    public override void Draw()
    {
        var state = Plugin.State;

        DrawStatusBanner();

        ImGui.Spacing();

        if (ImGui.CollapsingHeader("System Status", ImGuiTreeNodeFlags.DefaultOpen))
        {
            ImGui.BulletText($"Penumbra: {(state.PenumbraEnabled ? "Enabled" : "Disabled")}");

            ImGui.Separator();

            ImGui.BulletText($"Translation Status: {(state.Translation.Installed ? "Installed" : "Not Installed")}");

            ImGui.BulletText($"Translation Version: {state.Translation.Version}");

            ImGui.Separator();

            ImGui.BulletText($"Penumbra Mod Status: {(state.Penumbra.Installed ? "Installed" : "Not Installed")}");

            ImGui.BulletText($"Penumbra Mod Version: {state.Penumbra.Version}");
        }

        if (ImGui.CollapsingHeader("Version Information", ImGuiTreeNodeFlags.DefaultOpen))
        {
            ImGui.Text($"Translation Remote Version: {state.Translation.Version}");
            ImGui.Text($"Penumbra Remote Version: {state.Penumbra.Version}");

            ImGui.Text("Last Check: ");
            ImGui.SameLine();
            ImGui.TextDisabled(plugin.Configuration.LastUpdateCheck == default ? "Never" : plugin.Configuration.LastUpdateCheck.ToString("G"));
        }

        if (ImGui.CollapsingHeader("Last Changelog", ImGuiTreeNodeFlags.DefaultOpen))
        {
            ImGui.TextWrapped(Plugin.State.LastChangelog ?? "No changelog available.");
        }

        if (ImGui.CollapsingHeader("Actions", ImGuiTreeNodeFlags.DefaultOpen))
        {
            if (ImGui.Button("Refresh", new Vector2(-1, 0)))
            {
                refreshTask ??= Plugin.networkService.CheckForUpdates();
            }

            if (refreshTask?.IsCompleted == true)
            {
                refreshTask = null;
            }

            bool updateAvailable = Plugin.State.UpdateAvailable;

            using (ImRaii.Disabled(!updateAvailable))
            {
                if (ImGui.Button("Update components", new Vector2(-1, 0)))
                {
                    downloadTask ??= Plugin.networkService.DownloadLatestModAsync();
                }
            }

            if(downloadTask?.IsCompleted == true)
            {
                downloadTask = null;
            }

            if (ImGui.Button("Open config", new Vector2(-1, 0)))
            {
                plugin.ToggleConfigUi();
            }
        }

        if (ImGui.CollapsingHeader("Diagnostics"))
        {
            ImGui.TextDisabled("Branch: ");
            ImGui.SameLine();
            ImGui.Text(plugin.Configuration.Channel.ToString());

            ImGui.TextDisabled("Tester Access Allowance: ");
            ImGui.SameLine();

            if (!plugin.Configuration.TesterHumanCheck)
                ImGui.TextColored(ImGuiColors.DalamudYellow, "Not Allowed");
            else
                ImGui.TextColored(ImGuiColors.HealerGreen, "Allowed");
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
            ImGui.BeginChild("StatusBanner", new Vector2(-1, 50), true);

            using (ImRaii.PushColor(ImGuiCol.Text, color))
            {
                ImGui.SetCursorPosY(15);
                ImGui.TextUnformatted(text);
            }

            ImGui.EndChild();
        }
    }
}
