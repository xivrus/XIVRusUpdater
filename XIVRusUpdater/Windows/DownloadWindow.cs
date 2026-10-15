using Dalamud.Bindings.ImGui;
using Dalamud.Interface.Utility;
using Dalamud.Interface.Windowing;
using System;
using System.Numerics;
using XIVRusUpdater.Resource;
using XIVRusUpdater.Utils.States;

namespace XIVRusUpdater.Windows;

public class DownloadWindow : Window
{
    public DownloadWindow()
        : base($"{Localization.Downloading}###DownloadWindow")
    {
        SizeConstraints = new WindowSizeConstraints
        {
            MinimumSize = new Vector2(375, 330),
            MaximumSize = new Vector2(float.MaxValue, float.MaxValue)
        };

        RespectCloseHotkey = false;
    }

    public void Dispose() { }

    public override void Draw()
    {
        var download = Plugin.State.Penumbra.Download;

        if (download.IsDownloading) DrawDownloadInfo(download);
        
        download = Plugin.State.Translation.Download;

        if (download.IsDownloading) DrawDownloadInfo(download);
    }

    private void DrawDownloadInfo(DownloadState download)
    {
        ImGui.TextWrapped(download.FileName);

        ImGui.ProgressBar(download.Progress, new Vector2(-1, 24 * ImGuiHelpers.GlobalScale));

        ImGui.Spacing();

        ImGui.Text(string.Format(Localization.DownloadSize,
            Math.Round(download.DownloadedBytes / 1024f / 1024f),
            Math.Round(download.TotalBytes / 1024f / 1024f)));

        ImGui.TextWrapped(string.Format(Localization.CurrentSource, download.CurrentSource));

        ImGui.Text(string.Format(Localization.DownloadSpeed, Math.Round(download.SpeedMBps, 2)));
    }
}
