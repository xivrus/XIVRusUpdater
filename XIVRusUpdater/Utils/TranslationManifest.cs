using Newtonsoft.Json;
using System.Collections.Generic;

namespace XIVRusUpdater.Models;

public sealed class TranslationManifest
{
    [JsonProperty("changelog")]
    public string Changelog { get; set; } = string.Empty;
    [JsonProperty("game_version")]
    public string GameVersion { get; set; } = string.Empty;

    [JsonProperty("translation_version")] 
    public string TranslationVersion { get; set; } = string.Empty;
    [JsonProperty("translation_urls")]
    public List<string> TranslationUrls { get; set; } = [];
    [JsonProperty("translation_sha256")]
    public string TranslationHash { get; set; } = string.Empty;

    [JsonProperty("modpack_version")]
    public string ModpackVersion { get; set; } = string.Empty;
    [JsonProperty("modpack_urls")]
    public List<string> ModpackUrls { get; set; } = [];
    [JsonProperty("modpack_sha256")]
    public string ModpackHash { get; set; } = string.Empty;
}
