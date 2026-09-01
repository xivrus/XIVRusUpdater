using System;
using System.IO;
using Dalamud.Game.Command;
using Dalamud.Interface.Windowing;
using Dalamud.IoC;
using Dalamud.Plugin;
using Dalamud.Plugin.Services;
using XIVRusUpdater.Core;
using XIVRusUpdater.Hooks;
using XIVRusUpdater.Services;
using XIVRusUpdater.Utils.States;
using XIVRusUpdater.Windows;
using XIVRusUpdater.Windows.Debug;

namespace XIVRusUpdater;

public sealed class Plugin : IDalamudPlugin
{
    [PluginService] internal static IDalamudPluginInterface PluginInterface { get; private set; } = null!;

    [PluginService] internal static IFramework Framework { get; private set; } = null!;

    [PluginService] internal static ICommandManager CommandManager { get; private set; } = null!;

    [PluginService] internal static IDataManager DataManager { get; private set; } = null!;

    [PluginService] internal static IChatGui ChatGui { get; private set; } = null!;

    [PluginService] internal static INotificationManager NotificationManager { get; private set; } = null!;

    [PluginService] internal static IPluginLog Log { get; private set; } = null!;

    [PluginService] internal static IGameInteropProvider interopProvider { get; private set; } = null!;

    public static EXDHooks HookLayers { get; private set; } = null!;
    public static TranslationFilter filter { get; private set; } = null!;

    internal static PenumbraService PenumbraApi { get; private set; } = null!;
    internal static NetworkService networkService { get; private set; } = null!;
    internal static UpdaterState State { get; private set; } = null!;
    internal static Plugin Instance { get; private set; } = null!;

    private const string CommandName = "/xivrus";

    public Configuration Configuration { get; init; }
    private DateTime nextRefresh = DateTime.MinValue;


    public readonly WindowSystem WindowSystem = new("XIV Rus Updater");
    private readonly ConfigWindow ConfigWindow;
    private readonly MainWindow MainWindow;
    private readonly DownloadWindow DownloadWindow;
    private readonly ChangelogWindow Changelog;
    private readonly DebugWindow Debug;

    public Plugin()
    {
        Configuration = PluginInterface.GetPluginConfig() as Configuration ?? new Configuration();
        Instance = this;
        filter = new TranslationFilter();
        filter.Rebuild(Configuration.DisabledComponents);
        HookLayers = new EXDHooks(interopProvider, Configuration.EngineId);
        State = new UpdaterState();
        networkService = new NetworkService(this);
        PenumbraApi = new PenumbraService(PluginInterface);
        
        Framework.Update += OnUpdate;

        var iconPath = Path.Combine(PluginInterface.AssemblyLocation.Directory?.FullName!, "icon.png");

        ConfigWindow = new ConfigWindow(this);
        MainWindow = new MainWindow(this, iconPath);
        DownloadWindow = new DownloadWindow();
        Changelog = new ChangelogWindow(this);
        Debug = new DebugWindow();

        WindowSystem.AddWindow(ConfigWindow);
        WindowSystem.AddWindow(MainWindow);
        WindowSystem.AddWindow(DownloadWindow);
        WindowSystem.AddWindow(Changelog);
        WindowSystem.AddWindow(Debug);

        CommandManager.AddHandler(CommandName, new CommandInfo(OnCommand)
        {
            HelpMessage = "Open the plugin window. Use '/xivrus debug' for open debug window"
        });

        PluginInterface.UiBuilder.Draw += WindowSystem.Draw;
        PluginInterface.UiBuilder.OpenConfigUi += ToggleConfigUi;
        PluginInterface.UiBuilder.OpenMainUi += ToggleMainUi;
    }

    public void Dispose()
    {
        PluginInterface.UiBuilder.Draw -= WindowSystem.Draw;
        PluginInterface.UiBuilder.OpenConfigUi -= ToggleConfigUi;
        PluginInterface.UiBuilder.OpenMainUi -= ToggleMainUi;
        
        WindowSystem.RemoveAllWindows();

        HookLayers.Dispose();
        ConfigWindow.Dispose();
        MainWindow.Dispose();
        DownloadWindow.Dispose();

        CommandManager.RemoveHandler(CommandName);
    }

    private void OnCommand(string command, string args)
    {
        if (string.Equals(args.Trim(), "debug", StringComparison.OrdinalIgnoreCase))
        { 
            Debug.Toggle();
            return;
        }

        MainWindow.Toggle();
    }

    private async void OnUpdate(IFramework framework)
    {
        if (DateTime.Now > nextRefresh)
        {
            nextRefresh = DateTime.Now.AddMinutes(Configuration.UpdateCheckIntervalMinutes);
            Log.Information($"Perform timed update... Next update: {nextRefresh.ToString()}");

            _ = networkService.CheckForUpdates();
        }

        DownloadWindow.IsOpen = State.Penumbra.Download.IsDownloading || State.Translation.Download.IsDownloading;
        Changelog.IsOpen = State.ShowChangelog;
    }

    public void ToggleConfigUi() => ConfigWindow.Toggle();
    public void ToggleMainUi() => MainWindow.Toggle();
}
