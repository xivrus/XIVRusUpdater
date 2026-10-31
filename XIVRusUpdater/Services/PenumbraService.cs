using Dalamud.Plugin;
using Dalamud.Plugin.Ipc.Exceptions;
using System;
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

    public bool IsInstalled()
    {
        return pluginInterface.InstalledPlugins.Any(p => p.InternalName.Equals(InternalName, StringComparison.OrdinalIgnoreCase));
    }

    public bool IsPenumbraEnabled()
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

    public bool IsModInstalled(string modName) => ModList.Invoke().Any(x => x.Identifier == modName);

    public string? GetModVersion(string modName)
    {
        try 
        { 
            var mod = ModList.Invoke().FirstOrDefault(x => x.Identifier == modName);

            return mod.Version;
        }
        catch (IpcNotReadyError)
        {
            return null;
        }
    }

    public DirectoryInfo? GetModPath(string modName)
    {
        try
        {
            var mod = ModList.Invoke().FirstOrDefault(x => x.Identifier == modName);

            return mod.ModPath;
        }
        catch (IpcNotReadyError)
        {
            return null;
        }
    }

    public bool DeleteMod(string modName)
    {
        try 
        {
            var responce = DeleteMOD.Invoke(string.Empty, modName);
            if (responce == Penumbra.Api.Enums.PenumbraApiEc.Success) return true;
            Plugin.Log.Error($"Failed to delete mod: Recieve {Enum.GetName(responce)} from Penumbra");
            return false; 
        }
        catch(IpcNotReadyError)
        {
            return false;
        }
    }

    public bool InstallMod(string downloadPath)
    {
        try
        { 
            var response = InstallMOD.Invoke(downloadPath);

            if (response == Penumbra.Api.Enums.PenumbraApiEc.Success) return true;

            Plugin.Log.Error($"Failed to install mod: Recieve {Enum.GetName(response)} from Penumbra");
            return false;
        }
        catch (IpcNotReadyError)
        {
            return false;
        }
    }
}
