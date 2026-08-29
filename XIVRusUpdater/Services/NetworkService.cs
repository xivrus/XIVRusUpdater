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
using System.Threading.Tasks;
using XIVRusUpdater.Core.Components;
using XIVRusUpdater.Models;
using XIVRusUpdater.Utils.Extentions;
using XIVRusUpdater.Utils.States;
using static XIVRusUpdater.Utils.Extentions.HttpClientProgressExtensions;

namespace XIVRusUpdater.Services;

public class NetworkService
{
    private static readonly HttpClient Client = CreateClient();
    
    private readonly Plugin plugin;
    
    private static HttpClient CreateClient()
    {
        var client = new HttpClient
        {
            Timeout = TimeSpan.FromSeconds(10)
        };

        client.DefaultRequestHeaders.UserAgent.ParseAdd($"XIVRusUpdater/{Plugin.PluginInterface.Manifest.AssemblyVersion}");

        return client;
    }

    public string CurrentBranch()
    {
        var engine = TranslationEngines.Get(plugin.Configuration.EngineId);

        return plugin.Configuration.Channel == UpdateChannel.Beta ? $"{engine.ApiUrl}/branches/test" : $"{engine.ApiUrl}/branches/release";
    }

    public string CurrentXRT()
    {
        var engine = TranslationEngines.Get(plugin.Configuration.EngineId);

        return $"{engine!.ApiUrl}/branches/xrt";
    }

    public async Task<TranslationManifest?> GetBranchStatus()
    {
        var branch = CurrentBranch();

        using HttpResponseMessage responseMessage = await Client.GetAsync(branch);

        responseMessage.EnsureSuccessStatusCode();

        var status = JsonConvert.DeserializeObject<TranslationManifest>(await responseMessage.Content.ReadAsStringAsync());

        using HttpResponseMessage xrtMessage = await Client.GetAsync(CurrentXRT());

        xrtMessage.EnsureSuccessStatusCode();

        var xrtStatus = JsonConvert.DeserializeObject<TranslationManifest>(await xrtMessage.Content.ReadAsStringAsync());

        xrtStatus.PenumbraVersion = status.Version;
        xrtStatus.PenumbraChangelog = status.Changelog;
        xrtStatus.PenumbraDownloadUrls = status.DownloadUrl;

        Plugin.State.LastRemoteStatus = xrtStatus;
        return xrtStatus;
    }

    public async Task<string?> GetLastRemoteVersionAsync()
    {
        var response = await GetBranchStatus();
        if (response == null) return null;

        return response.Version;
    }

    public async Task<string?> GetLastRemotePenumbraAsync()
    {
        var response = await GetBranchStatus();
        if (response == null) return null;

        return response.PenumbraVersion;
    }

    public async Task CheckForUpdates()
    {
        Plugin.Log.Information("Update Check started");
        plugin.Configuration.LastUpdateCheck = DateTime.Now;
        
        await RefreshAsync();

        plugin.Configuration.LastSuccessfulUpdate = DateTime.Now;
        plugin.Configuration.Save();

        if (!Plugin.State.UpdateAvailable)
            return;

        if (!plugin.Configuration.AutoDownloadUpdates)
            return;

        if(Plugin.State.Penumbra.UpdateAvailable) await DownloadLatestModAsync();
        if (Plugin.State.Translation.UpdateAvailable) await DownloadLatestTranslationAsync();
    }

    public async Task DownloadLatestModAsync()
    {
        var release = Plugin.State.LastRemoteStatus;

        if(release == null) return;

        if(release.PenumbraVersion != null)
            plugin.Configuration.LastInstalledPenumbra = release.PenumbraVersion;
        
        var downloadSource = await GetFastestSource(release.PenumbraDownloadUrls);

        if (downloadSource == null)
            return;

        Plugin.Log.Information($"Starting download {downloadSource.FileName} from {downloadSource.Url}...");

        var tempFile = Path.Combine(Plugin.PenumbraApi.GetDefaultDirectory(), downloadSource.FileName);

        var success = await DownloadRemoteAsync(downloadSource.Url, tempFile, Plugin.State.Penumbra.Download);

        if (!success)
            return;

        Plugin.Log.Info($"Downloading {downloadSource.FileName} successful complete");

        if (!plugin.Configuration.AutoInstallUpdates)
            return;

        InstallDownloadedPenumbraAsync(tempFile);
        Plugin.State.Penumbra.UpdateAvailable = false;
    }

    public async Task DownloadLatestTranslationAsync()
    {
        var release = Plugin.State.LastRemoteStatus;

        if (release == null) return;

        var downloadSource = await GetFastestSource(release.DownloadUrl);

        if (downloadSource == null)
            return;

        Plugin.Log.Information($"Starting download {downloadSource.FileName} from {downloadSource.Url}...");

        var tempFile = Path.Combine(Path.GetTempPath(), downloadSource.FileName);

        var success = await DownloadRemoteAsync(downloadSource.Url, tempFile, Plugin.State.Translation.Download);

        if (!success)
            return;

        Plugin.Log.Info($"Downloading {downloadSource.FileName} successful complete");

        plugin.Configuration.LastInstalledVersion = release.Version;

        await InstallDownloadedVersionAsync(tempFile);

        File.Delete(tempFile);
        Plugin.State.Translation.UpdateAvailable = false;
    }

    public async Task InstallDownloadedVersionAsync(string filePath)
    {
        var resourceDir = Plugin.HookLayers.Parser.GetResourceDir();

        try
        {
            await ExtractFirePatchAsync(filePath, resourceDir);

            Plugin.Log.Information($"XIV Rus: firePatch extracted to {resourceDir}");
        }
        catch (Exception ex)
        {
            Plugin.Log.Error($"XIV Rus: failed to extract patch: {ex}");
            return;
        }

        Plugin.Log.Information($"XIV Rus has been extracted to resource path ({resourceDir}).");
    }

    public void InstallDownloadedPenumbraAsync(string filePath)
    {
        var engine = TranslationEngines.Get(plugin.Configuration.EngineId);

        Plugin.PenumbraApi.DeleteMod(engine!.ModName);

        bool isInstall = Plugin.PenumbraApi.InstallMod(filePath);
        Plugin.Log.Information($"XIV Rus has been queued for installation in Penumbra. Status: {isInstall}");
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

    public async Task RefreshAsync()
    {
        try
        {
            var engine = TranslationEngines.Get(plugin.Configuration.EngineId);

            Plugin.State.PenumbraEnabled = Plugin.PenumbraApi.IsPenumbraEnabled();

            var penumbraManifest = Plugin.State.Penumbra;
            var translationManifest = Plugin.State.Translation;

            penumbraManifest.Installed = Plugin.PenumbraApi.IsModInstalled(engine!.ModName);
            translationManifest.Installed = !Plugin.HookLayers.Parser.IsResourceEmpty();
    
            var remote = await GetLastRemoteVersionAsync() ?? "Unknown";

            plugin.Configuration.LastKnownRemoteVersion = translationManifest.RemoteVersion = remote;

            Plugin.State.Translation.Version = plugin.Configuration.LastInstalledVersion;

            if (plugin.Configuration.LastInstalledVersion != plugin.Configuration.LastKnownRemoteVersion)
                Plugin.State.Translation.UpdateAvailable = true;

            remote = await GetLastRemotePenumbraAsync() ?? "Unknown";

            plugin.Configuration.LastKnownRemotePenumbra = penumbraManifest.RemoteVersion = remote;

            plugin.Configuration.LastInstalledPenumbra = Plugin.State.Penumbra.Version = Plugin.PenumbraApi.GetModVersion(engine.ModName) ?? "Not installed";
            
            if (plugin.Configuration.LastInstalledPenumbra != plugin.Configuration.LastKnownRemotePenumbra)
                Plugin.State.Penumbra.UpdateAvailable = true;
        }
        catch (Exception ex)
        {
            Plugin.Log.Error(ex, "Refresh failed");
        }
    }

    private static async Task<DownloadSourceInfo?> GetFastestSource(IEnumerable<string> sources)
    {
        var tasks = sources.Select(async source =>
        {
            var stopwatch = new Stopwatch();
            try
            {
                var request = new HttpRequestMessage(HttpMethod.Get, source);
                request.Headers.Range = new RangeHeaderValue(0, 10 * 1024 * 1024);

                stopwatch.Start();
                using var response = await Client.SendAsync(request, HttpCompletionOption.ResponseHeadersRead);

                if(!response.IsSuccessStatusCode)
                    return (Url: source, FileName: string.Empty, SpeedMbps: 0.0, Success: false);

                byte[] content = await response.Content.ReadAsByteArrayAsync();
                stopwatch.Stop();

                double seconds = stopwatch.Elapsed.TotalSeconds;
                double speedMBps = (content.Length / (1024*1024)) / seconds;

                string? fileName = response.Content.Headers.ContentDisposition?.FileNameStar ?? response.Content.Headers.ContentDisposition?.FileName;

                if (string.IsNullOrWhiteSpace(fileName))
                    fileName = Path.GetFileName(new Uri(source).AbsolutePath);

                fileName = fileName?.Trim('"') ?? string.Empty;

                Plugin.Log.Debug($"Download link ({source}) checked. Speed: {speedMBps} MB/s");
                return (Url: source, FileName: fileName, SpeedMbps: speedMBps, Success: true);
            }
            catch
            {
                return (Url: source, FileName: string.Empty, SpeedMbps: 0.0, Success: false);
            }
        });

        var results = await Task.WhenAll(tasks);

        var bestSource = results.Where(x => x.Success).OrderByDescending(x => x.SpeedMbps).FirstOrDefault();

        if (!bestSource.Success)
            return null;

        Plugin.Log.Debug($"Best download source picked. Url: {bestSource.Url}, Speed: {bestSource.SpeedMbps}");

        return new DownloadSourceInfo
        {
            Url = bestSource.Url,
            FileName = bestSource.FileName
        };
    }

    public NetworkService(Plugin pluginRef)
    {
        plugin = pluginRef;
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
