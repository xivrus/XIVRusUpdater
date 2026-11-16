using Lumina.Excel.Sheets;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading;
using System.Threading.Tasks;
using XIVRusUpdater.Core.Components;
using XIVRusUpdater.Models;
using XIVRusUpdater.Utils.Extentions;
using XIVRusUpdater.Utils.States;
using static XIVRusUpdater.Utils.Extentions.HttpClientProgressExtensions;
using Action = System.Action;

namespace XIVRusUpdater.Services;

public class NetworkService
{
    private static readonly HttpClient Client = CreateClient();
    
    public event Action? OnTranslationInstalled;
    public event Action? OnPenumbraInstalled;

    private readonly SemaphoreSlim checkLock = new(1, 1);
    private const int SourceProbeSize = 2 * 1024 * 1024;

    private static HttpClient CreateClient()
    {
        var client = new HttpClient
        {
            Timeout = TimeSpan.FromSeconds(10)
        };

        client.DefaultRequestHeaders.UserAgent.ParseAdd($"XIVRusUpdater/{Plugin.PluginInterface.Manifest.AssemblyVersion}");

        return client;
    }

    private static IEnumerable<string> GetBranchSources()
    {
        var engine = TranslationEngines.Get(Plugin.Instance.Configuration.EngineId);

        if (engine == null || string.IsNullOrWhiteSpace(engine.ApiUrl))
            return Array.Empty<string>();

        var branch = Plugin.Instance.Configuration.Channel == UpdateChannel.Beta ? "test" : "release";
        var engineUrl = $"{engine.ApiUrl.TrimEnd('/')}/branches/{branch}.json";

        return Plugin.Instance.Configuration.Channel == UpdateChannel.Beta ? [engineUrl]
        : [engineUrl, $"https://github.com/xivrus/xiv_ru_weblate/releases/latest/{branch}.json"];
    }

    private static async Task<TranslationManifest?> FetchBranchAsync(string source, CancellationToken cancellationToken)
    {
        try
        {
            using var response = await Client.GetAsync(source, cancellationToken);
            response.EnsureSuccessStatusCode();

            var json = await response.Content.ReadAsStringAsync();
            var manifest = JsonConvert.DeserializeObject<TranslationManifest>(json);

            if (manifest == null || string.IsNullOrWhiteSpace(manifest.TranslationVersion) || string.IsNullOrWhiteSpace(manifest.ModpackVersion))
                return null;

            return manifest;
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            return null;
        }
        catch (Exception ex)
        {
            Plugin.Log.Warning($"Branch source failed: {source}. {ex.Message}");
            return null;
        }
    }

    private async Task<TranslationManifest?> RefreshBranchStatusAsync()
    {
        var sources = GetBranchSources().Where(x => !string.IsNullOrWhiteSpace(x)).Distinct().ToArray();

        if (sources.Length == 0)
            return null;

        using var cancellationTokenSource = new CancellationTokenSource();

        var pending = sources.Select(source => FetchBranchAsync(source, cancellationTokenSource.Token)).ToList();

        while (pending.Count > 0)
        {
            var completed = await Task.WhenAny(pending);
            pending.Remove(completed);

            var manifest = await completed;

            if (manifest == null)
                continue;

            cancellationTokenSource.Cancel();
            await Task.WhenAll(pending);

            return manifest;
        }

        return null;
    }

    public async Task CheckForUpdates()
    {
        if (!await checkLock.WaitAsync(0)) return;

        Plugin.State.IsChecking = true;
        try
        {
            Plugin.Log.Information("Update Check started");
            Plugin.Instance.Configuration.LastUpdateCheck = DateTime.Now;

            if (!await RefreshAsync()) return;
            Plugin.Instance.Configuration.LastSuccessfulUpdate = DateTime.Now;

            if (!Plugin.Instance.Configuration.AutoDownloadUpdates) return;

            var downloadTasks = new List<Task>();

            if (Plugin.State.Penumbra.UpdateAvailable) downloadTasks.Add(DownloadLatestModAsync());
            if (Plugin.State.Translation.UpdateAvailable) downloadTasks.Add(DownloadLatestTranslationAsync());

            if (downloadTasks.Count > 0)
                await Task.WhenAll(downloadTasks);
        }
        catch (Exception ex)
        {
            Plugin.Log.Error(ex, "CheckForUpdates failed");
        }
        finally
        {
            Plugin.Instance.Configuration.Save();
            Plugin.State.IsChecking = false;
            checkLock.Release();
        }
    }

    public async Task DownloadLatestModAsync()
    {
        var release = Plugin.State.LastRemoteStatus;
        if(release == null) return;
        
        var downloadSource = await GetFastestSource(release.ModpackUrls);
        if (downloadSource == null)
            return;

        Plugin.Log.Information($"Starting download {downloadSource.FileName} from {downloadSource.Url}...");

        var tempFile = Path.Combine(Plugin.PenumbraApi.GetDefaultDirectory() ?? Path.GetTempPath(), downloadSource.FileName);
        if (!await DownloadRemoteAsync(downloadSource.Url, tempFile, Plugin.State.Penumbra.Download))
            return;

        Plugin.Log.Info($"Downloading {downloadSource.FileName} successful complete");
        if (!InstallDownloadedPenumbraAsync(tempFile))
            return;

        Plugin.State.Penumbra.Version = Plugin.Instance.Configuration.LastInstalledPenumbra = release.ModpackVersion;
        Plugin.State.Penumbra.UpdateAvailable = false;
        
        OnPenumbraInstalled?.Invoke();
    }

    public async Task DownloadLatestTranslationAsync()
    {
        var release = Plugin.State.LastRemoteStatus;
        if (release == null) 
            return;

        var downloadSource = await GetFastestSource(release.TranslationUrls);
        if (downloadSource == null)
            return;

        Plugin.Log.Information($"Starting download {downloadSource.FileName} from {downloadSource.Url}...");

        var tempFile = Path.Combine(Path.GetTempPath(), downloadSource.FileName);
        if (!await DownloadRemoteAsync(downloadSource.Url, tempFile, Plugin.State.Translation.Download))
            return;

        Plugin.Log.Info($"Downloading {downloadSource.FileName} successful complete");

        var installed = await InstallDownloadedVersionAsync(tempFile);
        File.Delete(tempFile);
        if (!installed)
            return;

        Plugin.State.Translation.Version = Plugin.Instance.Configuration.LastInstalledVersion = release.TranslationVersion;
        Plugin.State.Translation.UpdateAvailable = false;
        Plugin.State.Translation.Installed = true;
        
        OnTranslationInstalled?.Invoke();
    }

    public async Task<bool> InstallDownloadedVersionAsync(string filePath)
    {
        var resourceDir = Plugin.HookLayers.parser.GetResourceDir();

        try
        {
            await ExtractFirePatchAsync(filePath, resourceDir);

            Plugin.Log.Information($"XIV Rus: firePatch extracted to {resourceDir}");
        }
        catch (Exception ex)
        {
            Plugin.Log.Error($"XIV Rus: failed to extract patch: {ex}");
            return false;
        }

        Plugin.Log.Information($"XIV Rus has been extracted to resource path ({resourceDir}).");
        return true;
    }

    public bool InstallDownloadedPenumbraAsync(string filePath)
    {
        var engine = TranslationEngines.Get(Plugin.Instance.Configuration.EngineId);

        Plugin.PenumbraApi.DeleteMod(engine!.ModName);

        bool isInstall = Plugin.PenumbraApi.InstallMod(filePath);
        Plugin.Log.Information($"XIV Rus has been queued for installation in Penumbra. Status: {isInstall}");
        return isInstall;
    }

    private static async Task ExtractFirePatchAsync(string zipPath, string resourceDir)
    {
        Directory.CreateDirectory(resourceDir);

        using var archive = ZipFile.OpenRead(zipPath);

        var root = Path.GetFullPath(resourceDir);

        foreach (var entry in archive.Entries)
        {
            if (string.IsNullOrEmpty(entry.Name))
                continue;

            var destinationPath = Path.GetFullPath(Path.Combine(resourceDir, entry.FullName));

            if (!destinationPath.StartsWith(root, StringComparison.OrdinalIgnoreCase))
            {
                throw new InvalidDataException($"Invalid path in archive: {entry.FullName}");
            }

            Directory.CreateDirectory(Path.GetDirectoryName(destinationPath)!);

            using var input = entry.Open();

            await using var output = new FileStream(destinationPath, FileMode.Create, FileAccess.Write, FileShare.None, bufferSize: 1024 * 64, useAsync: true);

            await input.CopyToAsync(output);
        }
    }

    public async Task<bool> RefreshAsync()
    {
        string? installedTranslationVersion = null;
        string? penumbraVersion = null;
        string? installedPenumbraVersion = null;

        try
        {
            Plugin.State.RemoteStatusKnown = false;

            var engine = TranslationEngines.Get(Plugin.Instance.Configuration.EngineId);

            if (engine == null)
                return false;

            Plugin.State.PenumbraEnabled = Plugin.PenumbraApi.IsPenumbraAvailable();

            var penumbraManifest = Plugin.State.Penumbra;
            var translationManifest = Plugin.State.Translation;

            penumbraManifest.Installed = Plugin.PenumbraApi.IsModInstalled(engine.ModName);
            translationManifest.Installed = !Plugin.HookLayers.parser.IsResourceEmpty();

            installedTranslationVersion = Plugin.HookLayers.parser.Version;
            if (string.IsNullOrWhiteSpace(installedTranslationVersion))
                installedTranslationVersion = Plugin.Instance.Configuration.LastInstalledVersion;

            penumbraVersion = Plugin.State.PenumbraEnabled && penumbraManifest.Installed ? Plugin.PenumbraApi.GetModVersion(engine.ModName) : null;

            installedPenumbraVersion = !string.IsNullOrWhiteSpace(penumbraVersion) ? penumbraVersion : Plugin.Instance.Configuration.LastInstalledPenumbra;

            var remote = await RefreshBranchStatusAsync();

            if (remote == null)
                return false;

            Plugin.Instance.Configuration.LastKnownRemoteVersion = translationManifest.RemoteVersion = remote.TranslationVersion;
            penumbraManifest.RemoteVersion = remote.ModpackVersion;

            if (!translationManifest.Installed)
            {
                translationManifest.UpdateAvailable = true;
            }
            else if (!string.IsNullOrWhiteSpace(installedTranslationVersion))
            {
                translationManifest.UpdateAvailable = installedTranslationVersion != remote.TranslationVersion;
            }

            if (!Plugin.State.PenumbraEnabled)
            {
                penumbraManifest.UpdateAvailable = false;
            }
            else if (!penumbraManifest.Installed)
            {
                penumbraManifest.UpdateAvailable = true;
            }
            else if (!string.IsNullOrWhiteSpace(installedPenumbraVersion))
            {
                penumbraManifest.UpdateAvailable = installedPenumbraVersion != remote.ModpackVersion;
            }

            Plugin.State.LastRemoteStatus = remote;
            Plugin.State.RemoteStatusKnown = true;
            return true;
        }
        catch (Exception ex)
        {
            Plugin.Log.Error(ex, "Refresh failed");
            return false;
        }
        finally
        {
            if (!string.IsNullOrWhiteSpace(installedTranslationVersion))
                Plugin.State.Translation.Version = installedTranslationVersion;

            if (!string.IsNullOrWhiteSpace(installedPenumbraVersion))
                Plugin.State.Penumbra.Version = installedPenumbraVersion;

            if (!string.IsNullOrWhiteSpace(penumbraVersion))
                Plugin.Instance.Configuration.LastInstalledPenumbra = penumbraVersion;
        }
    }

    private static async Task<DownloadSourceInfo?> GetFastestSource(IEnumerable<string> sources)
    {
        var tasks = sources.Select(async source =>
        {
            var stopwatch = new Stopwatch();
            try
            {
                using var request = new HttpRequestMessage(HttpMethod.Get, source);
                request.Headers.Range = new RangeHeaderValue(0, SourceProbeSize);

                stopwatch.Start();
                using var response = await Client.SendAsync(request, HttpCompletionOption.ResponseHeadersRead);

                if (!response.IsSuccessStatusCode)
                {
                    Plugin.Log.Warning($"Download source unavailable: {source}. HTTP {(int)response.StatusCode}");

                    return (Url: source, FileName: string.Empty, SpeedMbps: 0.0, Success: false);
                }

                byte[] content = await response.Content.ReadAsByteArrayAsync();
                stopwatch.Stop();

                double seconds = stopwatch.Elapsed.TotalSeconds;
                double speedMBps = content.Length / (1024.0*1024.0) / seconds;

                string? fileName = response.Content.Headers.ContentDisposition?.FileNameStar ?? response.Content.Headers.ContentDisposition?.FileName;

                if (string.IsNullOrWhiteSpace(fileName))
                    fileName = Path.GetFileName(new Uri(source).AbsolutePath);

                fileName = fileName?.Trim('"') ?? string.Empty;

                if (string.IsNullOrWhiteSpace(fileName))
                {
                    Plugin.Log.Warning($"Download source has no valid filename: {source}");

                    return ( Url: source, FileName: string.Empty, SpeedMbps: 0.0, Success: false );
                }

                Plugin.Log.Debug($"Download link ({source}) checked. Speed: {speedMBps} MB/s");
                return (Url: source, FileName: fileName, SpeedMbps: speedMBps, Success: true);
            }
            catch (OperationCanceledException ex)
            {
                Plugin.Log.Warning($"Download source timed out or was canceled: {source}. {ex.Message}");

                return (Url: source, FileName: string.Empty, SpeedMbps: 0.0, Success: false);
            }
            catch (HttpRequestException ex)
            {
                Plugin.Log.Warning($"Download source connection failed: {source}. {ex.Message}");

                return (Url: source, FileName: string.Empty, SpeedMbps: 0.0, Success: false);
            }
            catch (Exception ex)
            {
                Plugin.Log.Warning($"Download source check failed: {source}. {ex.Message}");

                return (Url: source, FileName: string.Empty, SpeedMbps: 0.0, Success: false);
            }
            finally
            {
                stopwatch.Stop();
            }
        });

        var results = await Task.WhenAll(tasks);

        var bestSource = results.Where(x => x.Success && x.SpeedMbps > 0).OrderByDescending(x => x.SpeedMbps).FirstOrDefault();

        if (!bestSource.Success)
        {
            Plugin.Log.Warning("No available download sources found.");
            return null; 
        }

        Plugin.Log.Debug($"Best download source picked. Url: {bestSource.Url}, Speed: {bestSource.SpeedMbps:F2}");

        return new DownloadSourceInfo
        {
            Url = bestSource.Url,
            FileName = bestSource.FileName
        };
    }

    public async Task<bool> DownloadRemoteAsync(string url, string targetFile, DownloadState state)
    {
        state.IsDownloading = true;
        state.CurrentSource = url;
        state.Error = null;
        state.FileName = Path.GetFileName(targetFile);
        state.DownloadedBytes = 0;
        state.TotalBytes = 0;
        state.SpeedMBps = 0;

        try
        {
            var progress = new Progress<DownloadProgressInfo>(p =>
            {
                state.DownloadedBytes = p.DownloadedBytes;
                state.TotalBytes = p.TotalBytes;
                state.SpeedMBps = p.SpeedMBps;
            });

            await using var file = File.Create(targetFile);

            await Client.DownloadDataAsync(url, file, progress);

            return true;
        }
        catch (Exception ex)
        {
            state.Error = ex.Message;
            Plugin.Log.Error($"Download Error: {ex.Message}");
            return false;
        }
        finally
        {
            state.IsDownloading = false;
        }
    }

    public sealed class DownloadSourceInfo
    {
        public string Url { get; init; } = string.Empty;

        public string FileName { get; init; } = string.Empty;
    }
}
