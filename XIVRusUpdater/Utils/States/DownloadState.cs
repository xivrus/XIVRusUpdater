using System;

namespace XIVRusUpdater.Utils.States;

public sealed class DownloadState
{
    public bool IsDownloading { get; set; }

    public string FileName { get; set; } = string.Empty;

    public string CurrentSource { get; set; } = string.Empty;

    public string? Error { get; set; } = null!;

    public long DownloadedBytes { get; set; }

    public long TotalBytes { get; set; }

    public double SpeedMBps { get; set; }

    public float Progress => TotalBytes == 0 ? 0 : Convert.ToSingle(DownloadedBytes) / TotalBytes;
}
