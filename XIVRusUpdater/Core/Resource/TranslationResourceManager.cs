using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using File = System.IO.File;

namespace XIVRusUpdater.Core.Resource;

public sealed class TranslationResourceManager
{
    public readonly string ResourceDir;
    public string TranslationDir => Path.Combine(ResourceDir, "exd");
    private readonly string _extension;
    public readonly string? Version;
    
    private readonly Dictionary<string, FileResource> _cache = new();
    
    public TranslationResourceManager(string dataDir, string engine, ResourceFormat format)
    {
        _extension = ResourceFormatParser.GetExtension(format);
        ResourceDir = Path.Combine(dataDir, engine, _extension);
 
        if (!Directory.Exists(ResourceDir))
            return;

        Version = GetVersion(Path.Combine(ResourceDir, "xivrus.ver"));

        var resourcesDir = string.IsNullOrEmpty(Version) ? ResourceDir : TranslationDir;
        foreach (var file in Directory.EnumerateFiles(resourcesDir, $"*.{_extension}", SearchOption.AllDirectories))
        {
            var relative = Path.GetRelativePath(resourcesDir, file);
            relative = Path.ChangeExtension(relative, null)!;

            var sheetName = relative.Replace(Path.DirectorySeparatorChar, '/');

            _cache[sheetName] = new FileResource(file, format, sheetName);
        }
    }

    private static string? GetVersion(string path)
    {
        if (!Path.Exists(path) || Path.GetExtension(path) != "ver")
            return null;

        return File.ReadAllText(path);
    }

    public TranslationResourceManager(string dataDir, string engine, string format)
        : this(dataDir, engine, ResourceFormatParser.Parse(format))
    {
    }

    public string GetResourceDir() => ResourceDir;

    public bool IsLoaded(string sheetName) => _cache.ContainsKey(sheetName);

    private string ToPath(string sheetName)
    {
        var relative = sheetName.Replace('/', Path.DirectorySeparatorChar) + $".{_extension}";
        return Path.Combine(ResourceDir, relative);
    }

    public bool TryGet(string sheetName, out FileResource resource) => _cache.TryGetValue(sheetName, out resource!);
}
