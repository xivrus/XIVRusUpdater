using Dalamud.Configuration;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;

namespace XIVRusUpdater;

[Serializable]
public class Configuration : IPluginConfiguration
{
    public int Version { get; set; } = 2;

    public string EngineId { get; set; } = "XIVRusEnglish";

    #region UI

    public bool ShowNotifications { get; set; } = true;

    public bool ShowChangelogAfterUpdate { get; set; } = true;

    public bool AllowSayInEnglish { get; set; } = true;

    #endregion

    #region Updates

    public int UpdateCheckIntervalMinutes { get; set; } = 60;

    public bool AutoDownloadUpdates { get; set; } = true;
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

    [JsonIgnore]
    public string LastInstalledVersion
    {
        get => CurrentEngineState.LastInstalledVersion;
        set => CurrentEngineState.LastInstalledVersion = value ?? string.Empty;
    }

    [JsonIgnore]
    public string LastInstalledPenumbra
    {
        get => CurrentEngineState.LastInstalledPenumbra;
        set => CurrentEngineState.LastInstalledPenumbra = value ?? string.Empty;
    }

    [JsonIgnore]
    public string LastKnownRemoteVersion
    {
        get => CurrentEngineState.LastKnownRemoteVersion;
        set => CurrentEngineState.LastKnownRemoteVersion = value ?? string.Empty;
    }

    [JsonIgnore]
    public string LastKnownRemotePenumbra
    {
        get => CurrentEngineState.LastKnownRemotePenumbra;
        set => CurrentEngineState.LastKnownRemotePenumbra = value ?? string.Empty;
    }

    [JsonIgnore]
    public DateTime LastUpdateCheck
    {
        get => CurrentEngineState.LastUpdateCheck;
        set => CurrentEngineState.LastUpdateCheck = value;
    }

    [JsonIgnore]
    public DateTime LastSuccessfulUpdate
    {
        get => CurrentEngineState.LastSuccessfulUpdate;
        set => CurrentEngineState.LastSuccessfulUpdate = value;
    }
    #endregion

    public HashSet<string> DisabledComponents { get; set; } = [];
    public HashSet<string> PluginCompatability { get; set; } = [];

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
