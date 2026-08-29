using Dalamud.Plugin;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using XIVRusUpdater.Core.Components;
using XIVRusUpdater.Core.Resource;
using XIVRusUpdater.Utils;

namespace XIVRusUpdater.Core;

public class TranslationParser : IDisposable
{
    private readonly object syncRoot = new();
    // TODO: обязательный перезапуск игры при смене движка в релизе?
    private readonly List<TranslationResourceManager> retiredResourceManagers = new();
    private TranslationResourceManager _ResourceManager { get; set; }
    private string _CurrentEngineId { get; set; }

    public TranslationParser(string @engineId)
    {
        _CurrentEngineId = engineId;
        _ResourceManager = CreateResourceManager(_CurrentEngineId);
    }

    public void UpdateEngine(string engineId)
    {
        lock (syncRoot)
        {
            if (engineId == _CurrentEngineId)
                return;
        }

        var newManager = CreateResourceManager(engineId);

        lock (syncRoot)
        {
            if (engineId == _CurrentEngineId)
            {
                newManager.Dispose();
                return;
            }

            retiredResourceManagers.Add(_ResourceManager);
            _ResourceManager = newManager;
            _CurrentEngineId = engineId;
        }
    }

    public bool IsResourceEmpty()
    {
        lock (syncRoot)
        {
            return !Directory.EnumerateFileSystemEntries(_ResourceManager.GetResourceDir()).Any();
        }
    }

    private static TranslationResourceManager CreateResourceManager(string engineId)
    {
        var engine = TranslationEngines.Get(engineId)
            ?? throw new ArgumentException($"Unknown translation engine: {engineId}", nameof(engineId));

        return new TranslationResourceManager(Plugin.PluginInterface.AssemblyLocation.Directory!.FullName, engine.Id, engine.Format);
    }

    public string GetResourceDir()
    {
        lock (syncRoot)
        {
            return _ResourceManager.GetResourceDir();
        }
    }

    public CacheMemoryStats GetCacheMemoryStats()
    {
        lock (syncRoot)
        {
            var active = _ResourceManager.GetCacheStats();
            int retiredResourceCount = 0;
            long retiredMemory = 0;

            foreach (var manager in retiredResourceManagers)
            {
                var retired = manager.GetCacheStats();
                retiredResourceCount += retired.ResourceCount;
                retiredMemory += retired.NativeMemoryBytes;
            }

            return new CacheMemoryStats(
                active.ResourceCount,
                active.NativeMemoryBytes,
                retiredResourceCount,
                retiredMemory,
                retiredResourceManagers.Count);
        }
    }

    public bool IsSheetLoaded(string sheetName)
    {
        lock (syncRoot)
            return _ResourceManager.IsLoaded(sheetName);
    }

    public bool TryGetValue(string sheetName, uint RowId, uint Column, out ByteArrayWrapper? translation)
    {
        lock (syncRoot)
        {
            translation = null;
            if (_ResourceManager.TryGet(sheetName, out var fileResource))
            {
                if (fileResource.TryGetData(RowId, Column, out var @byte))
                {
                    translation = @byte;
                    return true;
                }
            }

            return false;
        }
    }

    public void Dispose()
    {
        lock (syncRoot)
        {
            // Буферы могли быть возвращены игре и поэтому остаются жить до завершения процесса.
            retiredResourceManagers.Clear();
        }
    }
}

public readonly record struct CacheMemoryStats(
    int ActiveResourceCount,
    long ActiveNativeMemoryBytes,
    int RetiredResourceCount,
    long RetiredNativeMemoryBytes,
    int RetiredManagerCount)
{
    public long TotalNativeMemoryBytes => ActiveNativeMemoryBytes + RetiredNativeMemoryBytes;
}
