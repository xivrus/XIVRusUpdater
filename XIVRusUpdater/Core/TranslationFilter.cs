using System;
using System.Collections.Generic;
using XIVRusUpdater.Core.Components;

namespace XIVRusUpdater.Core;

public sealed class TranslationFilter
{
    private sealed class SheetState
    {
        public bool? WholeSheet;
        public Dictionary<uint, bool>? RowOverrides;
    }

    private volatile Dictionary<string, SheetState> _sheets = new(StringComparer.OrdinalIgnoreCase);
    private volatile HashSet<string> _activePrefixes = new(StringComparer.OrdinalIgnoreCase);
    private volatile HashSet<string> _allPrefixes = new(StringComparer.OrdinalIgnoreCase);

    public void Rebuild(IReadOnlySet<string> disabledComponents)
    {
        var sheets = new Dictionary<string, SheetState>(StringComparer.OrdinalIgnoreCase);
        var activePrefixes = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var allPrefixes = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        foreach (var component in TranslationComponents.All)
        {
            bool disabled = disabledComponents.Contains(component.Id);

            foreach (var (sheetName, rows) in component.Sheets)
            {
                if (sheetName.EndsWith('*'))
                {
                    var prefix = sheetName[..^1];

                    allPrefixes.Add(prefix);

                    if (!disabled)
                        activePrefixes.Add(prefix);
                }
                else
                {
                    if (!sheets.TryGetValue(sheetName, out var state))
                        sheets[sheetName] = state = new SheetState();

                    if (rows.Length == 0)
                    {
                        state.WholeSheet = (state.WholeSheet ?? false) || !disabled;
                    }
                    else
                    {
                        state.RowOverrides ??= new Dictionary<uint, bool>();
                        foreach (var row in rows)
                            state.RowOverrides[row] = !disabled;
                    }
                }
            }
        }

        _sheets = sheets;
        _activePrefixes = activePrefixes;
        _allPrefixes = allPrefixes;
    }

    public bool IsActive(string sheetName, uint rowId)
    {
        if (_sheets.TryGetValue(sheetName, out var state))
        {
            if (state.RowOverrides is { } overrides && overrides.TryGetValue(rowId, out var rowActive))
                return rowActive;

            if (state.WholeSheet is { } wholeSheetActive)
                return wholeSheetActive;

            return false;
        }

        foreach (var prefix in _activePrefixes)
        {
            if (sheetName.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
                return true;
        }

        foreach (var prefix in _allPrefixes)
        {
            if (sheetName.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
                return false;
        }

        return true;
    }
}
