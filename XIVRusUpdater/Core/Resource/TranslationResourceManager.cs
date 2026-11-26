using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using File = System.IO.File;

namespace XIVRusUpdater.Core.Resource;

public sealed class TranslationResourceManager
{
    public readonly string ResourceDir;
    
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

        var sw = Stopwatch.StartNew();
        var files = Directory.GetFiles(ResourceDir, $"*.{_extension}", SearchOption.AllDirectories);
        var results = new ConcurrentDictionary<string, FileResource>();
        var failed = 0;

        Parallel.ForEach(files, file => {
            var sheetName = Path.ChangeExtension(Path.GetRelativePath(ResourceDir, file), null)!
                .Replace(Path.DirectorySeparatorChar, '/');

            try
            {
                results[sheetName] = new FileResource(file, format, sheetName);
            }
            catch (Exception ex)
            {
                Interlocked.Increment(ref failed);
                Plugin.Log.Warning(ex, $"Failed to load {file}");
            }
        });

        foreach (var (name, resource) in results)
            _cache[name] = resource;

        Plugin.Log.Information($"Resources loaded: {_cache.Count}/{files.Length} sheets, {failed} failed, {sw.ElapsedMilliseconds} ms");
        Plugin.Notification.Warning($"Failed to load {failed} sheets");
    }

    private static string? GetVersion(string path)
    {
        if (!Path.Exists(path) || Path.GetExtension(path) != ".ver")
            return null;

        return File.ReadAllText(path).Trim();
    }

    public TranslationResourceManager(string dataDir, string engine, string format)
        : this(dataDir, engine, ResourceFormatParser.Parse(format))
    {
    }

    public bool IsLoaded(string sheetName) => _cache.ContainsKey(sheetName);

    private string ToPath(string sheetName)
    {
        var relative = sheetName.Replace('/', Path.DirectorySeparatorChar) + $".{_extension}";
        return Path.Combine(ResourceDir, relative);
    }

    public bool TryGet(string sheetName, out FileResource resource) => _cache.TryGetValue(sheetName, out resource!);
}
