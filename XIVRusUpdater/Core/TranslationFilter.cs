using System;
using System.Collections.Generic;
using XIVRusUpdater.Core.Components;
using static FFXIVClientStructs.FFXIV.Client.UI.RaptureAtkHistory.Delegates;

namespace XIVRusUpdater.Core;

public sealed class TranslationFilter
{
    private sealed class SheetState
    {
        public bool? WholeSheet;
        public Dictionary<uint, bool>? RowOverrides;
    }

    private volatile Dictionary<string, SheetState> _sheets = new(StringComparer.OrdinalIgnoreCase);
    private volatile Dictionary<string, bool> _prefixes = new(StringComparer.OrdinalIgnoreCase);
  
    public void Rebuild(IReadOnlySet<string> disabledComponents)
    {
        var sheets = new Dictionary<string, SheetState>(StringComparer.OrdinalIgnoreCase);
        var prefixes = new Dictionary<string, bool>(StringComparer.OrdinalIgnoreCase);
        
        foreach (var component in TranslationComponents.All)
        {
            bool enabled = !disabledComponents.Contains(component.Id);

            foreach (var (sheetName, rows) in component.Sheets)
            {
                if (sheetName.EndsWith('*'))
                {
                    var prefix = sheetName[..^1];
                    prefixes[prefix] = enabled || prefixes.GetValueOrDefault(prefix);
                    continue;
                }
                
                if (!sheets.TryGetValue(sheetName, out var state))
                    sheets[sheetName] = state = new SheetState();

                if (rows.Length == 0)
                {
                    state.WholeSheet = (state.WholeSheet ?? false) || enabled;
                }
                else
                {
                    state.RowOverrides ??= new Dictionary<uint, bool>();
                    foreach (var row in rows)
                        state.RowOverrides[row] = enabled;
                }
            }
        }

        _sheets = sheets;
        _prefixes = prefixes;
    }

    public bool IsActive(string sheetName, uint rowId)
    {
        if (_sheets.TryGetValue(sheetName, out var state))
        {
            if (state.RowOverrides is { } overrides && overrides.TryGetValue(rowId, out var rowActive))
                return rowActive;

            return state.WholeSheet ?? false;
        }

        var matched = false;
        foreach (var (prefix, enabled) in _prefixes)
        {
            if (!sheetName.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
                continue;

            if (enabled)
                return true;
            matched = true;
        }

        return !matched;
    }
}
