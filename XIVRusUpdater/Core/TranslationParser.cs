using Dalamud.Plugin;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using XIVRusUpdater.Core.Components;
using XIVRusUpdater.Core.Resource;
using XIVRusUpdater.Utils;

namespace XIVRusUpdater.Core;

public class TranslationParser
{
    private readonly object syncRoot = new();
    
    private readonly List<TranslationResourceManager> retired = new();
    private TranslationResourceManager _resources { get; set; }
    private static TranslationFilter filter { get; set; } = null!;

    private string _engineId { get; set; }

    public TranslationParser(string @engineId)
    {
        filter = new TranslationFilter();
        filter.Rebuild(Plugin.Instance.Configuration.DisabledComponents);
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

        return new TranslationResourceManager(Plugin.PluginInterface.GetPluginConfigDirectory(), engine.Id, engine.Format);
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

    public bool IsNeedTranslate(string sheetName, uint rowId)
    {
        return IsSheetLoaded(sheetName) && filter.IsActive(sheetName, rowId);
    }

    internal bool TryGetValue(string sheetName, uint RowId, uint Column, out Translation? translation)
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

    internal bool TryGetRowTranslations(string sheet, uint rowId, out Translation[] translations)
    {
        lock(syncRoot)
        {
            translations = null;
            if (_resources.TryGet(sheet, out var file) && file.TryGetData(rowId, out var columns))
            {
                translations = columns;
                return true;
            }

            return false;
        }
    }

    public void UpdateFilter() => filter.Rebuild(Plugin.Instance.Configuration.DisabledComponents);

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
                return;
            }

            retired.Add(_resources);
            _resources = next;
        }

        UpdateFilter();
    }
}
