using Newtonsoft.Json;
using System.Collections.Generic;

namespace XIVRusUpdater.Models;

public sealed class TranslationManifest
{
    [JsonProperty("version")] 
    public string Version { get; set; } = string.Empty;
    public string PenumbraVersion { get; set; } = string.Empty;

    [JsonProperty("changelog")] 
    public string Changelog { get; set; } = string.Empty;
    public string PenumbraChangelog {  get; set; } = string.Empty;

    [JsonProperty("urls")] 
    public List<string> DownloadUrl { get; set; } = [];
    public List<string> PenumbraDownloadUrls { get; set; } = [];
}
