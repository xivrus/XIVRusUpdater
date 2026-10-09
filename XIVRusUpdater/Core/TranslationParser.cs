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
    
    private readonly List<TranslationResourceManager> retired = new();
    private TranslationResourceManager _resources { get; set; }
    private string _engineId { get; set; }

    public TranslationParser(string @engineId)
    {
        _engineId = engineId;
        _resources = CreateResourceManager(_engineId);
    }

    public void UpdateEngine(string engineId)
    {
        lock (syncRoot)
        {
            if (engineId == _engineId)
                return;
        }

        var newManager = CreateResourceManager(engineId);

        lock (syncRoot)
        {
            if (engineId == _engineId)
            {
                newManager.Dispose();
                return;
            }

            retired.Add(_resources);
            _resources = newManager;
            _engineId = engineId;
        }
    }

    public bool IsResourceEmpty()
    {
        lock (syncRoot)
        {
            var dir = _resources.GetResourceDir();
            return !Directory.Exists(dir) || !Directory.EnumerateFileSystemEntries(dir).Any();
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
            return _resources.GetResourceDir();
        }
    }

    public bool IsSheetLoaded(string sheetName)
    {
        lock (syncRoot)
            return _resources.IsLoaded(sheetName);
    }

    public bool TryGetValue(string sheetName, uint RowId, uint Column, out ByteArrayWrapper? translation)
    {
        lock (syncRoot)
        {
            translation = null;
            if (_resources.TryGet(sheetName, out var fileResource) && fileResource.TryGetData(RowId, Column, out var @byte))
            {
                translation = @byte;
                return true;
            }

            return false;
        }
    }

    public void Reload()
    {
        string id;
        lock (syncRoot)
            id = _engineId;

        var next = CreateResourceManager(id);

        lock (syncRoot)
        {
            if (id != _engineId)
            {
                next.Dispose();
                return;
            }

            retired.Add(_resources);
            _resources = next;
        }
    }

    public void Dispose()
    {
        lock (syncRoot)
        {
            foreach (var manager in retired)
                manager.Dispose();
            retired.Clear();
            _resources.Dispose();
        }
    }
}
