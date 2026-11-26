using Dalamud.Game.ClientState.Conditions;
using Dalamud.Game.Command;
using Dalamud.Interface.Windowing;
using Dalamud.IoC;
using Dalamud.Plugin;
using Dalamud.Plugin.Services;
using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using XIVRusUpdater.Core;
using XIVRusUpdater.Core.Components;
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
    [PluginService] internal static INotificationManager NotificationManager { get; private set; } = null!;
    [PluginService] internal static IDataManager DataManager { get; private set; } = null!;
    [PluginService] internal static ISigScanner SigScanner { get; private set; } = null!;
    [PluginService] internal static IPluginLog Log { get; private set; } = null!;
    [PluginService] internal static IGameInteropProvider interopProvider { get; private set; } = null!;
    [PluginService] internal static ICondition condition { get; private set;  } = null!;

    public static EXDHooks ExcelLayer { get; private set; } = null!;
    
    internal static PenumbraService PenumbraApi { get; private set; } = null!;
    internal static NetworkService networkService { get; private set; } = null!;
    internal static NotificationService Notification { get; private set; } = null!;
    internal static UpdaterState State { get; private set; } = null!;
    internal static Plugin Instance { get; private set; } = null!;

    private const string CommandName = "/xivrus";

    public Configuration Configuration { get; init; }
    public CancellationToken LifetimeToken => lifetime.Token;
    private readonly CancellationTokenSource lifetime = new();
    private Task? updateLoop;

    public readonly WindowSystem WindowSystem = new("XIV Rus Updater");
    private readonly ConfigWindow ConfigWindow;
    private readonly MainWindow MainWindow;
    private readonly DownloadWindow DownloadWindow;
    private readonly ChangelogWindow Changelog;
#if DEBUG
    private readonly DebugWindow Debug;
#endif

    public Plugin()
    {
        Instance = this;
        Configuration = PluginInterface.GetPluginConfig() as Configuration ?? new Configuration();
        TranslationComponents.Initialize(DataManager);
        Notification = new NotificationService(NotificationManager);
        ExcelLayer = new EXDHooks(interopProvider, SigScanner, Configuration.EngineId);
        State = new UpdaterState();
        networkService = new NetworkService();
        
        PenumbraApi = new PenumbraService(PluginInterface);
        
        ConfigWindow = new ConfigWindow();
        MainWindow = new MainWindow();
        DownloadWindow = new DownloadWindow();
        Changelog = new ChangelogWindow();
#if DEBUG
        Debug = new DebugWindow();
        WindowSystem.AddWindow(Debug);
#endif

        WindowSystem.AddWindow(ConfigWindow);
        WindowSystem.AddWindow(MainWindow);
        WindowSystem.AddWindow(DownloadWindow);
        WindowSystem.AddWindow(Changelog);
        
        CommandManager.AddHandler(CommandName, new CommandInfo(OnCommand)
        {
            HelpMessage = "Open the plugin window. Use '/xivrus settings' for open settings window"
        });

        PluginInterface.UiBuilder.Draw += DrawUI;
        PluginInterface.UiBuilder.OpenConfigUi += ToggleConfigUi;
        PluginInterface.UiBuilder.OpenMainUi += ToggleMainUi;
        networkService.OnTranslationInstalled += UpdateTranslation;

        updateLoop = Task.Run(() => UpdateLoopAsync(lifetime.Token));
    }

    private void DrawUI()
    {
        DownloadWindow.IsOpen = State.Penumbra.Download.IsDownloading || State.Translation.Download.IsDownloading;
        Changelog.IsOpen = State.ShowChangelog;
        WindowSystem.Draw();
    }

    public void Dispose()
    {
        lifetime.Cancel();

        PluginInterface.UiBuilder.Draw -= DrawUI;
        PluginInterface.UiBuilder.OpenConfigUi -= ToggleConfigUi;
        PluginInterface.UiBuilder.OpenMainUi -= ToggleMainUi;
        networkService.OnTranslationInstalled -= UpdateTranslation;
        
        WindowSystem.RemoveAllWindows();

        ExcelLayer.Dispose();
        ConfigWindow.Dispose();
        MainWindow.Dispose();
        DownloadWindow.Dispose();

        CommandManager.RemoveHandler(CommandName);
    }

    private void OnCommand(string command, string args)
    {
#if DEBUG
        if (string.Equals(args.Trim(), "debug", StringComparison.OrdinalIgnoreCase))
        {
            ToggleDebugUi();
            return;
        }
#endif
        if (string.Equals(args.Trim(), "settings", StringComparison.OrdinalIgnoreCase))
        {
            ToggleConfigUi();
            return;
        }

        ToggleMainUi();
    }

    public static bool BoundByDuty() => condition[ConditionFlag.BoundByDuty] || condition[ConditionFlag.BoundByDuty56] || condition[ConditionFlag.BoundByDuty95];

    private async Task UpdateLoopAsync(CancellationToken ct)
    {
        try
        {
            if (!ExcelLayer.parser.IsResourceEmpty())
                await Task.Delay(TimeSpan.FromSeconds(30), ct);

            while(!ct.IsCancellationRequested)
            {
                var inDuty = await Framework.RunOnFrameworkThread(BoundByDuty);

                if (!inDuty)
                    await RunCheckAsync();

                var delay = inDuty ? TimeSpan.FromMinutes(1) : TimeSpan.FromMinutes(Math.Max(1, Configuration.UpdateCheckIntervalMinutes));

                Log.Information($"Next update check in {delay.TotalMinutes:F0} min");
                await Task.Delay(delay, ct);
            }
        }
        catch (OperationCanceledException) { }
        catch (Exception ex) 
        { 
            Log.Error(ex, "Update loop failed");
            Notification.Error($"An error occured in update loop");
        }
    }

    private async Task RunCheckAsync()
    {
        try 
        { 
            await networkService.CheckForUpdates(lifetime.Token); 
        }
        catch (OperationCanceledException) 
        {
            Log.Warning("Update check canceled by token");
            Notification.Warning("Update check canceled by token");
        }
        catch (Exception ex) 
        { 
            Log.Error(ex, "Update check failed");
            Notification.Error("Failed to perform update check");
        }
    }

    public Task ForceUpdateCheck() => networkService.CheckForUpdates(lifetime.Token);

    public void UpdateTranslation() => ExcelLayer.parser.Reload();

    public void ToggleConfigUi() => ConfigWindow.Toggle();
    public void ToggleMainUi() => MainWindow.Toggle();

#if DEBUG
    public void ToggleDebugUi() => Debug.Toggle();
#endif
}
