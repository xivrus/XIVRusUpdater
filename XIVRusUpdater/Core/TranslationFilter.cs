using System;
using System.Collections.Generic;
using XIVRusUpdater.Core.Components;

namespace XIVRusUpdater.Core;

public sealed class TranslationFilter
{
    private sealed class SheetState
    {
        public bool Whole;
        public HashSet<uint>? Rows;
    }

    private volatile Dictionary<string, SheetState> _sheets = new(StringComparer.OrdinalIgnoreCase);
    private volatile List<string> _prefixes = new();

    public void Rebuild(IReadOnlySet<string> disabledComponents)
    {
        var sheets = new Dictionary<string, SheetState>(StringComparer.OrdinalIgnoreCase);
        var prefixes = new List<string>();

        foreach (var component in TranslationComponents.All)
        {
            if (!disabledComponents.Contains(component.Id))
                continue;

            foreach (var (sheetName, rows) in component.Sheets)
            {
                if (sheetName.EndsWith('*'))
                {
                    prefixes.Add(sheetName[..^1]);
                    continue;
                }

                if (!sheets.TryGetValue(sheetName, out var state))
                    sheets[sheetName] = state = new SheetState();

                if (rows.Length == 0)
                    state.Whole = true;
                else
                    (state.Rows ??= new HashSet<uint>()).UnionWith(rows);
            }
        }

        _sheets = sheets;
        _prefixes = prefixes;
    }

    public bool IsActive(string sheetName, uint rowId)
    {
        if (_sheets.TryGetValue(sheetName, out var state) && (state.Whole || state.Rows?.Contains(rowId) == true))
            return false;

        foreach (var prefix in _prefixes)
        {
            if (sheetName.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
                return false;
        }

        return true;
    }
}
