using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using static System.Net.WebRequestMethods;

namespace XIVRusUpdater.Core.Resource;

public sealed class TranslationResourceManager : IDisposable
{
    private readonly string _resourceDir;
    private readonly string _extension;

    private readonly Dictionary<string, FileResource> _cache = new();
    
    public TranslationResourceManager(string dataDir, string engine, ResourceFormat format)
    {
        _extension = ResourceFormatParser.GetExtension(format);
        _resourceDir = Path.Combine(dataDir, engine, _extension);

        if (!Directory.Exists(_resourceDir))
            return;

        foreach (var file in Directory.EnumerateFiles(_resourceDir, $"*.{_extension}", SearchOption.AllDirectories))
        {
            var relative = Path.GetRelativePath(_resourceDir, file);
            relative = Path.ChangeExtension(relative, null)!;

            var sheetName = relative.Replace(Path.DirectorySeparatorChar, '/');

            _cache[sheetName] = new FileResource(file, format, sheetName);
        }
    }

    public TranslationResourceManager(string dataDir, string engine, string format)
        : this(dataDir, engine, ResourceFormatParser.Parse(format))
    {
    }

    public string GetResourceDir() => _resourceDir;

    public bool IsLoaded(string sheetName) => _cache.ContainsKey(sheetName);

    private string ToPath(string sheetName)
    {
        var relative = sheetName.Replace('/', Path.DirectorySeparatorChar) + $".{_extension}";
        return Path.Combine(_resourceDir, relative);
    }

    public bool TryGet(string sheetName, out FileResource resource)
        => _cache.TryGetValue(sheetName, out resource!);

    public (int ResourceCount, long NativeMemoryBytes) GetCacheStats()
    {
        long memory = 0;
        foreach (var resource in _cache.Values)
            memory += resource.GetNativeMemoryUsage();

        return (_cache.Count, memory);
    }

    public void Dispose()
    {
        foreach (var (_, file) in _cache)
            file.Dispose();

        _cache.Clear();
    }
}
