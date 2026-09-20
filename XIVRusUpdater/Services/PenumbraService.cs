using Dalamud.Plugin;
using Dalamud.Plugin.Ipc.Exceptions;
using FFXIVClientStructs.FFXIV.Client.Graphics.Kernel;
using Luna;
using Penumbra.Api.Enums;
using Penumbra.Api.IpcSubscribers;
using Penumbra.Api.IpcSubscribers.Legacy;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace XIVRusUpdater.Services;

public sealed class PenumbraService
{
    private Penumbra.Api.IpcSubscribers.GetModList GetListMod { get; } = null!;
    private Penumbra.Api.IpcSubscribers.InstallMod InstallMod { get; } = null!;
    private Penumbra.Api.IpcSubscribers.DeleteMod DeleteMod { get; } = null!;
    private Penumbra.Api.IpcSubscribers.ReloadMod ReloadMod { get; } = null!;
    private Penumbra.Api.IpcSubscribers.GetEnabledState GetEnableStatus { get; } = null!;
    private Penumbra.Api.IpcSubscribers.GetModListAdapterOld ModList { get; } = null!;
    private Penumbra.Api.IpcSubscribers.GetCollection GetCollection { get; } = null!;
    private Penumbra.Api.IpcSubscribers.TrySetMod ChangeEnabled { get; } = null!;
    private Penumbra.Api.IpcSubscribers.GetCurrentModSettings ModSettings { get; } = null!;
    private Penumbra.Api.IpcSubscribers.GetModDirectory GetDirectory { get; } = null!;
    private EventSubscriber Init { get; } = null!;
    private Penumbra.Api.IpcSubscribers.GetCollections GetCollections { get; } = null!;

    private const string InternalName = "Penumbra";
    private const string RepoUrl =
        "https://raw.githubusercontent.com/xivdev/Penumbra/master/repo.json";

    public PenumbraService(IDalamudPluginInterface @interface)
    {
        GetListMod = new (@interface);
        InstallMod = new (@interface);
        DeleteMod = new (@interface);
        ReloadMod = new (@interface);
        GetEnableStatus = new (@interface);
        ModList = new (@interface);
        GetCollection = new (@interface);
        ChangeEnabled = new (@interface);
        ModSettings = new (@interface);
        GetDirectory = new (@interface);
        GetCollections = new (@interface);

        Init = Initialized.Subscriber(@interface, OnPenumbraInitialized);
        Init.Enable();
        try
        {
            if (IsEnabled())
                OnPenumbraInitialized();
        }
        catch(IpcNotReadyError)
        {
            // Do nothing, OnPenumbraInitialized do all works
            Plugin.Log.Information("IPC Not Ready... Waiting Penumbra...");
        }
    }

    private void OnPenumbraInitialized()
    {
        Plugin.Log.Debug("Penumbra initialized... Perform checks");
        _ = CheckAndDisableIfNeeded();
    }

    public async Task<bool> EnsureInstalledAsync()
    {
        return await Plugin.dalamud.EnsurePluginInstalledAsync(InternalName, RepoUrl);
    }

    public bool IsInstalled()
    {
        return Plugin.PluginInterface.InstalledPlugins.Any(p => p.InternalName.Equals(InternalName, StringComparison.OrdinalIgnoreCase));
    }

    public async Task CheckAndDisableIfNeeded()
    {
        var mod = Plugin.State.mod;
        var remoteVersion = Plugin.State.LastRemoteStatus;
        if (remoteVersion is null) return;

        var localVersion = Plugin.CurrentGameVersion;

        Plugin.Log.Debug($"Remote Game: {remoteVersion.GameVersion}. Local: {localVersion}");

        if (remoteVersion.GameVersion == localVersion)
        {
            if (!mod.Enabled) mod.Enabled = true;
            return; 
        }

        Plugin.Log.Information("Disabling XIV Rus due GameVersion missmatch");
        mod.Enabled = false;
    }

    public bool IsEnabled() => GetEnableStatus.Invoke();
    
    private Guid? GetDefaultCollection()
    {
        return GetCollection.Invoke(Penumbra.Api.Enums.ApiCollectionType.Default)?.Id;
    }

    public string GetDefaultDirectory() => GetDirectory.Invoke();

    public bool IsModInstalled(string modName) => GetListMod.Invoke().ContainsKey(modName);
    
    public string? GetModVersion(string modName)
    {
        if (!IsModInstalled(modName)) return null;

        var modList = ModList.Invoke();
        var mod = modList.FirstOrDefault(x => x.Identifier == modName);
        var modVersion = mod.Version;
        return modVersion;
    }

    public DirectoryInfo? GetModPath(string modName)
    {
        if (!IsModInstalled(modName)) return null;

        var modList = ModList.Invoke();
        var mod = modList.FirstOrDefault(x => x.Identifier == modName);
        return mod.ModPath;
    }

    public bool IsModEnabled(string modName)
    {
        var collectionId = GetDefaultCollection();
        if (collectionId is null) return false;

        var modSettings = ModSettings.Invoke(collectionId.Value, string.Empty, modName);

        switch(modSettings.Item1)
        {
            case Penumbra.Api.Enums.PenumbraApiEc.Success:
                return modSettings.Item2?.Item1 ?? false;
            case Penumbra.Api.Enums.PenumbraApiEc.InvalidArgument:
            case Penumbra.Api.Enums.PenumbraApiEc.CollectionMissing:
            case Penumbra.Api.Enums.PenumbraApiEc.ModMissing:
                Plugin.Log.Error($"Failed to determinate enabled state: Recieve {Enum.GetName(modSettings.Item1)} from Penumbra");
                break;
        }

        return false;
    }

    public bool SetModEnabled(string modName, bool enabled)
    {
        var collections = GetCollections.Invoke();
        var anyFailed = false;

        foreach (var (collectionId, _) in collections)
        {
            var response = ChangeEnabled.Invoke(collectionId, string.Empty, enabled, modName);
            if (response is not PenumbraApiEc.Success and not PenumbraApiEc.NothingChanged)
                anyFailed = true;
        }

        return !anyFailed;
    }

    public bool DeleteMods(string modName)
    {
        var responce = DeleteMod.Invoke(string.Empty, modName);

        if (responce == Penumbra.Api.Enums.PenumbraApiEc.Success) return true;

        Plugin.Log.Error($"Failed to delete plugin: Recieve {Enum.GetName(responce)} from Penumbra");
        return false;
    }

    public bool ReloadMods(string modName)
    {
        var response = ReloadMod.Invoke(string.Empty, modName);

        if (response == Penumbra.Api.Enums.PenumbraApiEc.Success) return true;

        Plugin.Log.Error($"Failed to reload plugin: Recieve {Enum.GetName(response)} from Penumbra");
        return false;
    }

    public bool InstallMods(string downloadPath)
    {
        var response = InstallMod.Invoke(downloadPath);

        if (response == Penumbra.Api.Enums.PenumbraApiEc.Success) return true;

        Plugin.Log.Error($"Failed to install plugin: Recieve {Enum.GetName(response)} from Penumbra");
        return false;
    }
}
