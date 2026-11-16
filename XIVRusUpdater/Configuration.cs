using Dalamud.Configuration;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Runtime.Serialization;
using XIVRusUpdater.Core.Components;

namespace XIVRusUpdater;

[Serializable]
public class Configuration : IPluginConfiguration
{
    public int Version { get; set; } = 2;

    public string EngineId { get; set; } = "XIVRusEnglish";

    #region UI

    public bool ShowNotifications { get; set; } = true;

    public bool ShowChangelogAfterUpdate { get; set; } = true;

    #endregion

    #region Updates

    public int UpdateCheckIntervalMinutes { get; set; } = 60;

    public bool AutoDownloadUpdates { get; set; } = true;

    public bool BlockUpdateWhileDuty { get; set; } = false;

    #endregion

    #region Tester Access

    public bool TesterHumanCheck { get; set; }

    public UpdateChannel Channel { get; set; } = UpdateChannel.Stable;

    #endregion

    #region State

    [JsonProperty("updateStates")]
    public Dictionary<string, UpdateInfo> UpdateStates { get; set; } = new();

    private UpdateInfo CurrentEngineState
    {
        get
        {
            UpdateStates ??= new Dictionary<string, UpdateInfo>();

            if (string.IsNullOrWhiteSpace(EngineId))
                EngineId = "XIVRusEnglish";

            if (!UpdateStates.TryGetValue(EngineId, out var info) || info is null)
            {
                info = new UpdateInfo();
                UpdateStates[EngineId] = info;
            }

            return info;
        }
    }

    #region Legacy
    [JsonProperty("LastInstalledVersion", NullValueHandling = NullValueHandling.Ignore)]
    private string? LegacyLastInstalledVersion { get; set; }

    [JsonProperty("LastInstalledPenumbra", NullValueHandling = NullValueHandling.Ignore)]
    private string? LegacyLastInstalledPenumbra { get; set; }

    [JsonProperty("LastKnownRemoteVersion", NullValueHandling = NullValueHandling.Ignore)]
    private string? LegacyLastKnownRemoteVersion { get; set; }

    [JsonProperty("LastKnownRemotePenumbra", NullValueHandling = NullValueHandling.Ignore)]
    private string? LegacyLastKnownRemotePenumbra { get; set; }

    [JsonProperty("LastUpdateCheck", NullValueHandling = NullValueHandling.Ignore)]
    private DateTime? LegacyLastUpdateCheck { get; set; }

    [JsonProperty("LastSuccessfulUpdate", NullValueHandling = NullValueHandling.Ignore)]
    private DateTime? LegacyLastSuccessfulUpdate { get; set; }
    #endregion

    #endregion

    public HashSet<string> DisabledComponents { get; set; } = [];

    public void Save()
    {
        Plugin.PluginInterface.SavePluginConfig(this);
    }
}

public class UpdateInfo
{
    public string LastInstalledVersion { get; set; } = string.Empty;
    public string LastInstalledPenumbra { get; set; } = string.Empty;

    public string LastKnownRemoteVersion { get; set; } = string.Empty;
    public string LastKnownRemotePenumbra { get; set; } = string.Empty;

    public DateTime LastUpdateCheck { get; set; }
    public DateTime LastSuccessfulUpdate { get; set; }
}

public enum UpdateChannel
{
    Stable,
    Beta
}
