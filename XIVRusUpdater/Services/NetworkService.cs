using Lumina.Data;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading.Tasks;
using XIVRusUpdater;
using XIVRusUpdater.Models;
using XIVRusUpdater.Utils;
using static XIVRusUpdater.Utils.HttpClientProgressExtensions;

namespace XIVRusUpdater.Services;

public class NetworkService
{
    private static readonly HttpClient Client = CreateClient();
    
    private readonly Plugin plugin;
    
    public enum AvailabilityStatus
    {
        Available,
        Disabled
    }

    private static HttpClient CreateClient()
    {
        var client = new HttpClient
        {
            Timeout = TimeSpan.FromSeconds(10)
        };

        client.DefaultRequestHeaders.UserAgent.ParseAdd($"XIVRusUpdater/{Plugin.PluginInterface.Manifest.AssemblyVersion}");

        return client;
    }

    public string CurrentBranch() => plugin.Configuration.Channel == UpdateChannel.Beta ? $"{Plugin.State.mod.API_BASE}/branches/test.json" 
        : $"{Plugin.State.mod.API_BASE}/branches/release.json";

    public async Task<XIVStatus?> GetBranchStatus()
    {
        var branch = CurrentBranch();

        using HttpResponseMessage responseMessage = await Client.GetAsync(branch);

        responseMessage.EnsureSuccessStatusCode();

        var status = JsonConvert.DeserializeObject<XIVStatus>(await responseMessage.Content.ReadAsStringAsync());

        Plugin.State.LastRemoteStatus = status;
        Plugin.State.LastChangelog = status?.Changelog;
        Plugin.Log.Information($"Fetched latest info. Game Version: {status?.GameVersion}, XIV Rus Version: {status?.RusVersion}");
        return status;
    }

    public async Task<AvailabilityStatus> GetStatusAsync()
    {
        var xivstatus = await GetBranchStatus();

        if (xivstatus?.GameVersion != Plugin.CurrentGameVersion) return AvailabilityStatus.Disabled;
        else return AvailabilityStatus.Available;
    }

    public async Task<string?> GetLastRemoteVersionAsync()
    {
        var response = await GetBranchStatus();
        if (response == null) return null;

        return response.RusVersion;
    }

    public async Task CheckForUpdates()
    {
        Plugin.Log.Information("Update Check started");
        plugin.Configuration.LastUpdateCheck = DateTime.Now;

        await RefreshAsync();

        plugin.Configuration.LastSuccessfulUpdate = DateTime.Now;

        if (!Plugin.State.UpdateAvailable)
            return;

        if (!plugin.Configuration.AutoDownloadUpdates)
            return;

        await DownloadLatestVersionAsync();
    }

    public async Task DownloadLatestVersionAsync()
    {
        var release = await GetBranchStatus();

        if(release == null) return;

        if(release.RusVersion != null)
            plugin.Configuration.LastInstalledVersion = release.RusVersion;
        
        var downloadSource = await GetFastestSource(release.Urls);

        if (downloadSource == null)
            return;

        Plugin.Log.Information($"Starting download {downloadSource.FileName} from {downloadSource.Url}...");

        var tempFile = Path.Combine(Plugin.PenumbraApi.GetDefaultDirectory(), downloadSource.FileName);

        var success = await DownloadModAsync(downloadSource.Url, tempFile);

        if (!success)
            return;

        Plugin.Log.Info($"Downloading {downloadSource.FileName} successful complete");

        if (!plugin.Configuration.AutoInstallUpdates)
            return;

        InstallDownloadedVersionAsync(tempFile);

        Plugin.State.ShowChangelog = true;
    }

    public void InstallDownloadedVersionAsync(string filePath)
    {
        Plugin.PenumbraApi.DeleteMods(Plugin.State.mod.modName);

        bool isInstall = Plugin.PenumbraApi.InstallMods(filePath);
        Plugin.Log.Information($"XIV Rus has been queued for installation in Penumbra. Status: {isInstall}");
    }

    public async Task RefreshAsync()
    {
        try
        {
            Plugin.State.Availability = await GetStatusAsync();

            Plugin.State.PenumbraEnabled = Plugin.PenumbraApi.IsEnabled();
            Plugin.State.ModInstalled = Plugin.PenumbraApi.IsModInstalled(Plugin.State.mod.modName);
    
            var remote = await GetLastRemoteVersionAsync();

            plugin.Configuration.LastKnownRemoteVersion = remote ?? "Unknown";

            string modVersion = Plugin.PenumbraApi.GetModVersion(Plugin.State.mod.modName) ?? "Not installed";
            
            Plugin.State.InstalledVersion = modVersion;

            plugin.Configuration.LastInstalledVersion = Plugin.State.InstalledVersion;

            Plugin.State.RemoteVersion = remote ?? "Unknown";
            
            Plugin.State.UpdateAvailable = remote != null && remote != Plugin.State.InstalledVersion;
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

    public async Task<bool> DownloadModAsync(string url, string targetFile)
    {
        var state = Plugin.State.Download;

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
            Plugin.State.ShowChangelog = true;
            state.IsDownloading = false;
        }
    }

    public sealed class DownloadSourceInfo
    {
        public string Url { get; init; } = string.Empty;

        public string FileName { get; init; } = string.Empty;
    }
}
