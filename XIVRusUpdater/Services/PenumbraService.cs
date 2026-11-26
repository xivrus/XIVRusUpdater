using Dalamud.Plugin;
using Dalamud.Plugin.Ipc.Exceptions;
using Penumbra.Api.Wrappers;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace XIVRusUpdater.Services;

public sealed class PenumbraService
{
    private Penumbra.Api.IpcSubscribers.InstallMod InstallMOD { get; } = null!;
    private Penumbra.Api.IpcSubscribers.DeleteMod DeleteMOD { get; } = null!;
    private Penumbra.Api.IpcSubscribers.GetEnabledState GetEnableStatus { get; } = null!;
    private Penumbra.Api.IpcSubscribers.GetModListAdapterOld ModList { get; } = null!;
    private Penumbra.Api.IpcSubscribers.GetModDirectory GetDirectory { get; } = null!;
    
    private const string InternalName = "Penumbra";
    private readonly IDalamudPluginInterface pluginInterface;

    public PenumbraService(IDalamudPluginInterface @interface)
    {
        pluginInterface = @interface;

        InstallMOD = new (@interface);
        DeleteMOD = new (@interface);
        GetEnableStatus = new (@interface);
        ModList = new (@interface);
        GetDirectory = new (@interface);
    }

    private bool IsInstalled()
    {
        return pluginInterface.InstalledPlugins.Any(p => p.InternalName.Equals(InternalName, StringComparison.OrdinalIgnoreCase));
    }

    private bool IsPenumbraEnabled()
    {
        try
        { 
            return GetEnableStatus.Invoke();
        }
        catch (IpcNotReadyError)
        {
            return false;
        }
    }

    private bool TryGetMod(string modName, out ModWrapperOld mod)
    {
        try
        {
            var mods = ModList.Invoke();

            foreach (var item in mods)
            {
                if (item.Identifier != modName)
                    continue;

                mod = item;
                return true;
            }

            mod = default;
            return false;
        }
        catch (IpcNotReadyError)
        {
            mod = default;
            return false;
        }
    }

    public string? GetDefaultDirectory()
    {
        try
        {
            return GetDirectory.Invoke();
        }
        catch (IpcNotReadyError)
        {
            return null;
        }
    }

    public bool IsModInstalled(string modName) => TryGetMod(modName, out _);

    public bool IsPenumbraAvailable()
    {
        return IsInstalled() && IsPenumbraEnabled();
    }

    public string? GetModVersion(string modName) => TryGetMod(modName, out var mod) ? mod.Version : null;

    public DirectoryInfo? GetModPath(string modName) => TryGetMod(modName, out var mod) ? mod.ModPath : null;

    public bool DeleteMod(string modName)
    {
        if (!IsPenumbraAvailable())
            return false;

        try
        {
            var response = DeleteMOD.Invoke(string.Empty, modName);
            if (response == Penumbra.Api.Enums.PenumbraApiEc.Success) return true;
            Plugin.Log.Error($"Failed to delete mod: Recieve {Enum.GetName(response)} from Penumbra");
            if (response != Penumbra.Api.Enums.PenumbraApiEc.NothingChanged)
                Plugin.Notification.Error("Failed to delete mod");
            return false; 
        }
        catch(IpcNotReadyError)
        {
            Plugin.Log.Error("Failed to delete mod: IPC Not Ready");
            return false;
        }
    }

    public bool InstallMod(string downloadPath)
    {
        if (!IsPenumbraAvailable())
            return false;

        try
        { 
            var response = InstallMOD.Invoke(downloadPath);

            if (response == Penumbra.Api.Enums.PenumbraApiEc.Success) return true;

            Plugin.Log.Error($"Failed to install mod: Recieve {Enum.GetName(response)} from Penumbra");
            if (response != Penumbra.Api.Enums.PenumbraApiEc.NothingChanged)
                Plugin.Notification.Error("Failed to install mod");
            return false;
        }
        catch (IpcNotReadyError)
        {
            Plugin.Log.Error("Failed to install mod: IPC Not Ready");
            return false;
        }
    }
}
