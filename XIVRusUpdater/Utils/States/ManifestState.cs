using System;
using System.Collections.Generic;
using System.Text;

namespace XIVRusUpdater.Utils.States;
    
public sealed class ManifestState
{
    public bool Installed { get; set; }

    public string? Version { get; set; }

    public string? RemoteVersion { get; set; }

    public bool UpdateAvailable { get; set; }

    public bool ShowChangelog { get; set; }

    public string? LastChangelog { get; set; }

    public DownloadState Download {  get; init; } = new DownloadState();
}
