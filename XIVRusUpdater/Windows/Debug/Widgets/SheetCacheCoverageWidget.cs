using Dalamud.Bindings.ImGui;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using XIVRusUpdater.Hooks;
using XIVRusUpdater.Utils;

namespace XIVRusUpdater.Windows.Debug.Widgets;

public sealed class SheetCacheCoverageWidget : IDebugWindowWidget
{
    public string[]? CommandShortcuts { get; init; } = ["cache", "coverage"];
    public string DisplayName { get; init; } = "Sheet Cache";
    public bool Ready { get; set; }

    private CacheSnapshotEntry[] snapshot = [];
    private LruCacheEntryView<nint, ColumnInfo>[] evictionCandidates = [];
    private DateTime lastLoadedAt;
    private string? lastError;

    private string? selectedSheet;
    private SheetDetail selectedDetail;
    private string sheetDetailError = string.Empty;

    private string sheetFilter = string.Empty;
    private int sheetSortColumn = 1;
    private bool sheetSortAscending = false;

    private string evictFilter = string.Empty;
    private int evictSortColumn = 0;
    private bool evictSortAscending;

    private int statEntryCount;
    private int statCapacity;
    private double statFillRatio;
    private long statEvicted;
    private int statSchemaCount;

    private bool autoRefresh = true;
    private static readonly TimeSpan AutoRefreshInterval = TimeSpan.FromSeconds(1.5);

    public void Load()
    {
        try
        {
            snapshot = Plugin.HookLayers.GetCacheSnapshot();
            evictionCandidates = Plugin.HookLayers.GetColumnCacheEvictionCandidates();
            statEntryCount = Plugin.HookLayers.ColumnCacheCount;
            statCapacity = Plugin.HookLayers.ColumnCacheCapacity;
            statFillRatio = Plugin.HookLayers.ColumnCacheFillRatio;
            statEvicted = Plugin.HookLayers.ColumnCacheEvictedCount;
            statSchemaCount = Plugin.HookLayers.StringColumnCacheCount;
            if (selectedSheet is not null)
            {
                try
                {
                    selectedDetail = Plugin.HookLayers.GetSheetDetail(selectedSheet);
                    sheetDetailError = string.Empty;
                }
                catch (Exception ex)
                {
                    sheetDetailError = ex.Message;
                }
            }
            lastError = null;
        }
        catch (Exception ex)
        {
            snapshot = [];
            evictionCandidates = [];
            lastError = ex.Message;
        }
        finally
        {
            lastLoadedAt = DateTime.Now;
            Ready = true;
        }
    }

    public void Draw()
    {
        if (!Ready)
        {
            ImGui.TextDisabled("Column cache not loaded yet.");
            ImGui.SameLine();
            if (ImGui.SmallButton("Load##cache_coverage"))
                Load();
            return;
        }

        if (lastError is not null)
        {
            ImGui.TextColored(new Vector4(0.90f, 0.30f, 0.30f, 1f), $"Cache read error: {lastError}");
            ImGui.SameLine();
            if (ImGui.SmallButton("Retry##cache_coverage"))
                Load();
            return;
        }

        var totalRows = snapshot.Sum(s => s.RowCount);

        if (autoRefresh && DateTime.Now - lastLoadedAt > AutoRefreshInterval)
            Load();

        ImGui.TextDisabled($"Snapshot {lastLoadedAt:T} · {snapshot.Length} sheets · {totalRows} rows cached");
        ImGui.SameLine();
        if (ImGui.SmallButton("Refresh##cache_coverage"))
            Load();
        ImGui.SameLine();
        ImGui.Checkbox("Auto##cache_coverage", ref autoRefresh);

        ImGui.Separator();

        DrawStatCells(
            ("Entries", $"{statEntryCount} / {statCapacity}"),
            ("Fill", $"{statFillRatio:P1}"),
            ("Evicted (LRU)", statEvicted.ToString()),
            ("Schemas", statSchemaCount.ToString()));

        ImGui.ProgressBar((float)statFillRatio, new Vector2(-1, 0), $"{statFillRatio:P1} full");

        if (DrawSheetTable())
            DrawSheetDetail();
        DrawEvictionCandidates();
    }

    private static void DrawStatCells(params (string Label, string Value)[] stats)
    {
        if (!ImGui.BeginTable("##cache_coverage_stats", stats.Length,
                ImGuiTableFlags.Borders | ImGuiTableFlags.SizingStretchSame))
            return;

        foreach (var (label, value) in stats)
        {
            ImGui.TableNextColumn();
            ImGui.TextDisabled(label);
            ImGui.SameLine();
            ImGui.TextUnformatted(value);
        }

        ImGui.EndTable();
    }

    private bool DrawSheetTable()
    {
        if (!ImGui.CollapsingHeader("Sheets##cache_coverage", ImGuiTreeNodeFlags.DefaultOpen))
            return false;

        if (snapshot.Length == 0)
        {
            ImGui.TextDisabled("No cached sheets.");
            return true;
        }

        ImGui.InputTextWithHint("##sheet_filter", "Filter by sheet name...", ref sheetFilter, 128);

        var sheets = string.IsNullOrWhiteSpace(sheetFilter)
            ? snapshot
            : snapshot.Where(s => s.SheetName.Contains(sheetFilter, StringComparison.OrdinalIgnoreCase)).ToArray();

        if (!ImGui.BeginTable("##sheet_cache_coverage_table", 3,
                ImGuiTableFlags.Borders | ImGuiTableFlags.RowBg | ImGuiTableFlags.Sortable))
            return true;

        ImGui.TableSetupColumn("Sheet");
        ImGui.TableSetupColumn("Cached rows");
        ImGui.TableSetupColumn("Unique columns");
        ImGui.TableHeadersRow();

        var sortSpecs = ImGui.TableGetSortSpecs();
        if (sortSpecs is { SpecsDirty: true })
        {
            sheetSortColumn = sortSpecs.Specs.ColumnIndex;
            sheetSortAscending = sortSpecs.Specs.SortDirection == ImGuiSortDirection.Ascending;
            sortSpecs.SpecsDirty = false;
        }

        var ordered = SortSheets(sheets, sheetSortColumn, sheetSortAscending);

        foreach (var entry in ordered)
        {
            ImGui.TableNextRow();
            ImGui.TableNextColumn();
            if (ImGui.Selectable(entry.SheetName, selectedSheet == entry.SheetName,
                    ImGuiSelectableFlags.SpanAllColumns | ImGuiSelectableFlags.AllowItemOverlap))
            {
                ToggleSheet(entry.SheetName);
            }
            ImGui.TableNextColumn();
            ImGui.TextUnformatted(entry.RowCount.ToString());
            ImGui.TableNextColumn();
            ImGui.TextUnformatted(entry.ColumnCount.ToString());
        }

        ImGui.EndTable();
        return true;
    }

    private static CacheSnapshotEntry[] SortSheets(CacheSnapshotEntry[] source, int column, bool ascending)
    {
        return column switch
        {
            0 => ascending
                ? source.OrderBy(e => e.SheetName, StringComparer.Ordinal).ToArray()
                : source.OrderByDescending(e => e.SheetName, StringComparer.Ordinal).ToArray(),
            1 => ascending
                ? source.OrderBy(e => e.RowCount).ToArray()
                : source.OrderByDescending(e => e.RowCount).ToArray(),
            2 => ascending
                ? source.OrderBy(e => e.ColumnCount).ToArray()
                : source.OrderByDescending(e => e.ColumnCount).ToArray(),
            _ => source,
        };
    }

    private void ToggleSheet(string sheetName)
    {
        if (selectedSheet == sheetName)
        {
            selectedSheet = null;
            return;
        }

        try
        {
            selectedSheet = sheetName;
            selectedDetail = Plugin.HookLayers.GetSheetDetail(sheetName);
            sheetDetailError = string.Empty;
        }
        catch (Exception ex)
        {
            selectedSheet = null;
            sheetDetailError = ex.Message;
        }
    }

    private void DrawSheetDetail()
    {
        if (selectedSheet is null)
            return;

        if (!string.IsNullOrEmpty(sheetDetailError))
        {
            ImGui.TextColored(new Vector4(0.90f, 0.30f, 0.30f, 1f), $"Sheet read error: {sheetDetailError}");
            ImGui.SameLine();
            if (ImGui.SmallButton("Close##sheet_detail"))
                selectedSheet = null;
            return;
        }

        ImGui.Separator();
        ImGui.TextUnformatted($"Sheet: {selectedDetail.SheetName} — {selectedDetail.RowCount} cached rows");
        ImGui.SameLine();
        if (ImGui.SmallButton("Close##sheet_detail"))
            selectedSheet = null;

        if (selectedDetail.Rows.Length == 0)
        {
            ImGui.TextDisabled("No cached rows for this sheet.");
            return;
        }

        var globalIndicesBySheet = Plugin.HookLayers.GetStringColumnIndicesCacheSnapshot()
            .ToDictionary(e => e.Key, e => e.Value, StringComparer.Ordinal);
        globalIndicesBySheet.TryGetValue(selectedDetail.SheetName, out var globalIndices);

        if (!ImGui.BeginTable("##sheet_detail_table", 3,
                ImGuiTableFlags.Borders | ImGuiTableFlags.RowBg | ImGuiTableFlags.ScrollY,
                new Vector2(0, 280)))
            return;

        ImGui.TableSetupColumn("Row ID", ImGuiTableColumnFlags.WidthFixed, 80);
        ImGui.TableSetupColumn("String col #", ImGuiTableColumnFlags.WidthStretch);
        ImGui.TableSetupColumn("Excel col #", ImGuiTableColumnFlags.WidthStretch);
        ImGui.TableHeadersRow();

        foreach (var row in selectedDetail.Rows)
        {
            var indicesText = string.Join(", ", row.ColumnIndices);
            var globalText = globalIndices is null
                ? "-"
                : string.Join(", ", row.ColumnIndices.Select(i =>
                    i < globalIndices.Length ? globalIndices[i].ToString() : "?"));

            ImGui.TableNextRow();
            ImGui.TableNextColumn();
            ImGui.TextUnformatted(row.RowId.ToString());

            ImGui.TableNextColumn();
            ImGui.TextWrapped(indicesText);

            ImGui.TableNextColumn();
            ImGui.TextWrapped(globalText);
        }

        ImGui.EndTable();
    }

    private void DrawEvictionCandidates()
    {
        if (!ImGui.CollapsingHeader("LRU eviction candidates##cache_coverage"))
            return;

        if (evictionCandidates.Length == 0)
        {
            ImGui.TextDisabled("Cache is empty.");
            return;
        }

        DrawEvictionLegend();

        ImGui.InputTextWithHint("##evict_filter", "Filter by sheet name...", ref evictFilter, 128);

        var filtered = string.IsNullOrWhiteSpace(evictFilter)
            ? evictionCandidates
            : evictionCandidates.Where(e => e.Value.SheetName.Contains(evictFilter, StringComparison.OrdinalIgnoreCase)).ToArray();

        if (!ImGui.BeginTable("##eviction_candidates_table", 6,
                ImGuiTableFlags.Borders | ImGuiTableFlags.RowBg | ImGuiTableFlags.Sortable | ImGuiTableFlags.ScrollY,
                new Vector2(0, 320)))
            return;

        ImGui.TableSetupColumn("Proximity", ImGuiTableColumnFlags.DefaultSort);
        ImGui.TableSetupColumn("Rank");
        ImGui.TableSetupColumn("Sheet");
        ImGui.TableSetupColumn("Row");
        ImGui.TableSetupColumn("Column");
        ImGui.TableSetupColumn("Address");
        ImGui.TableHeadersRow();

        var sortSpecs = ImGui.TableGetSortSpecs();
        if (sortSpecs is { SpecsDirty: true })
        {
            evictSortColumn = sortSpecs.Specs.ColumnIndex;
            evictSortAscending = sortSpecs.Specs.SortDirection == ImGuiSortDirection.Ascending;
            sortSpecs.SpecsDirty = false;
        }

        foreach (var entry in SortEviction(filtered, evictSortColumn, evictSortAscending))
        {
            var proximity = entry.EvictionProximity;
            var color = LerpEvictionColor(proximity);
            var info = entry.Value;

            ImGui.TableNextRow();
            ImGui.TableNextColumn();
            ImGui.TextColored(color, $"{proximity:P1}");
            ImGui.TableNextColumn();
            ImGui.TextUnformatted(entry.AgeRank.ToString());
            ImGui.TableNextColumn();
            ImGui.TextUnformatted(info.SheetName);
            ImGui.TableNextColumn();
            ImGui.TextUnformatted(info.RowId.ToString());
            ImGui.TableNextColumn();
            ImGui.TextUnformatted(info.ColumnIndex.ToString());
            ImGui.TableNextColumn();
            ImGui.TextUnformatted($"0x{(ulong)entry.Key:X}");
        }

        ImGui.EndTable();
    }

    private static void DrawEvictionLegend()
    {
        var drawList = ImGui.GetWindowDrawList();
        var pos = ImGui.GetCursorScreenPos();
        const float barWidth = 180f;
        const float barHeight = 10f;
        const int steps = 36;

        for (var i = 0; i < steps; i++)
        {
            var t = (float)i / (steps - 1);
            var color = LerpEvictionColor(t);
            drawList.AddRectFilled(
                pos + new Vector2(t * barWidth, 0f),
                pos + new Vector2((t + 1f / steps) * barWidth, barHeight),
                ImGui.ColorConvertFloat4ToU32(color));
        }

        ImGui.Dummy(new Vector2(barWidth, barHeight));
        ImGui.SameLine();
        ImGui.TextDisabled("fresh (0%) → evict soon (100%)");
    }

    private static LruCacheEntryView<nint, ColumnInfo>[] SortEviction(
        LruCacheEntryView<nint, ColumnInfo>[] source, int column, bool ascending)
    {
        return column switch
        {
            0 => ascending
                ? source.OrderBy(e => e.EvictionProximity).ToArray()
                : source.OrderByDescending(e => e.EvictionProximity).ToArray(),
            1 => ascending
                ? source.OrderBy(e => e.AgeRank).ToArray()
                : source.OrderByDescending(e => e.AgeRank).ToArray(),
            2 => ascending
                ? source.OrderBy(e => e.Value.SheetName, StringComparer.Ordinal).ToArray()
                : source.OrderByDescending(e => e.Value.SheetName, StringComparer.Ordinal).ToArray(),
            3 => ascending
                ? source.OrderBy(e => e.Value.RowId).ToArray()
                : source.OrderByDescending(e => e.Value.RowId).ToArray(),
            4 => ascending
                ? source.OrderBy(e => e.Value.ColumnIndex).ToArray()
                : source.OrderByDescending(e => e.Value.ColumnIndex).ToArray(),
            5 => ascending
                ? source.OrderBy(e => e.Key).ToArray()
                : source.OrderByDescending(e => e.Key).ToArray(),
            _ => source,
        };
    }

    private static Vector4 LerpEvictionColor(double proximity)
    {
        var p = (float)Math.Clamp(proximity, 0d, 1d);
        return new Vector4(p, 1f - p, 0.25f, 1f);
    }
}
