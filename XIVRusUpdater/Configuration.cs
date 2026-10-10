using Dalamud.Configuration;
using System;
using System.Collections.Generic;
using XIVRusUpdater.Core.Components;

namespace XIVRusUpdater;

[Serializable]
public class Configuration : IPluginConfiguration
{
    public int Version { get; set; } = 1;

    public string EngineId { get; set; } = "XIVRusEnglish";

    #region UI

    public bool ShowNotifications { get; set; } = true;

    public bool ShowChangelogAfterUpdate { get; set; } = true;

    #endregion

    #region Updates

    public int UpdateCheckIntervalMinutes { get; set; } = 60;

    public bool AutoDownloadUpdates { get; set; } = true;

    public bool AutoInstallUpdates { get; set; } = true;

    #endregion

    #region Tester Access

    public bool TesterHumanCheck { get; set; }

    public UpdateChannel Channel { get; set; } = UpdateChannel.Stable;

    #endregion

    #region State

    public string LastInstalledVersion { get; set; } = string.Empty;
    public string LastInstalledPenumbra { get; set;  } = string.Empty;

    public string LastKnownRemoteVersion { get; set; } = string.Empty;
    public string LastKnownRemotePenumbra { get; set; } = string.Empty;

    public DateTime LastUpdateCheck { get; set; }

    public DateTime LastSuccessfulUpdate { get; set; }

    #endregion

    #region Components
    public HashSet<string> DisabledComponents { get; set; } = [];

    public bool IsComponentEnabled(string id) => !DisabledComponents.Contains(id);

    public void SetComponentEnabled(string id, bool enabled)
    {
        if (enabled)
            DisabledComponents.Remove(id);
        else
            DisabledComponents.Add(id);
    }

    #endregion

    public void Save()
    {
        Plugin.PluginInterface.SavePluginConfig(this);
    }
}

public enum UpdateChannel
{
    Stable,
    Beta
}
