using System;
using System.Collections.Generic;
using System.Linq;
using XIVRusUpdater.Utils;

namespace XIVRusUpdater.Hooks;

public readonly record struct CacheSnapshotEntry(string SheetName, int RowCount, int ColumnCount);

public readonly record struct SheetRowDetail(uint RowId, uint[] ColumnIndices, int EntryCount);

public readonly record struct SheetDetail(string SheetName, int RowCount, SheetRowDetail[] Rows);

public unsafe partial class EXDHooks
{
    public CacheSnapshotEntry[] GetCacheSnapshot()
    {
        var items = columnMap.Snapshot();

        return items.GroupBy(info => info.SheetName).Select(group => new CacheSnapshotEntry(
            SheetName: group.Key,
            RowCount: group.Select(i => i.RowId).Distinct().Count(),
            ColumnCount: group.Select(i => i.ColumnIndex).Distinct().Count())).OrderByDescending(e => e.RowCount).ToArray();
    }

    public SheetDetail GetSheetDetail(string sheetName)
    {
        var items = columnMap.Snapshot()
            .Where(info => info.SheetName == sheetName)
            .ToArray();

        var rows = items
            .GroupBy(info => info.RowId)
            .OrderBy(group => group.Key)
            .Select(group => new SheetRowDetail(
                RowId: group.Key,
                ColumnIndices: group.Select(i => i.ColumnIndex).OrderBy(c => c).Distinct().ToArray(),
                EntryCount: group.Count()))
            .ToArray();

        return new SheetDetail(
            SheetName: sheetName,
            RowCount: rows.Length,
            Rows: rows);
    }

    public LruCacheEntryView<nint, ColumnInfo>[] GetColumnCacheEvictionCandidates()
        => columnMap.GetEntriesOrderedByRecency()
            .OrderByDescending(entry => entry.EvictionProximity)
            .ToArray();
}
