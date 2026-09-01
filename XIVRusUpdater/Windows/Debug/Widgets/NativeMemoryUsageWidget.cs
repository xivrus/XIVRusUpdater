using Dalamud.Bindings.ImGui;
using Dalamud.Hooking;
using System;
using System.Linq;
using System.Numerics;
using System.Runtime.CompilerServices;
using XIVRusUpdater.Hooks;
using static FFXIVClientStructs.FFXIV.Client.LayoutEngine.LayoutManager;

namespace XIVRusUpdater.Windows.Debug.Widgets;

public sealed class NativeMemoryUsageWidget : IDebugWindowWidget
{
    public string[]? CommandShortcuts { get; init; } = ["memory"];
    public string DisplayName { get; init; } = "Native Memory Usage";
    public bool Ready { get; set; }

    private long totalNativeBytes;
    private long retiredNativeBytes;
    private int activeResourceCount;
    private int retiredResourceCount;
    private int retiredManagerCount;
    private int columnCacheCount;
    private int sheetSchemaCacheCount;
    private (string SheetName, uint[] StringColumnIndices)[] sheetSchema = [];
    private string filter = string.Empty;
    private bool showSchemaBreakdown;

    private DateTime lastLoadedAt;
    private string? lastError;

    public void Load()
    {
        Refresh();
        Ready = true;
    }

    private void Refresh()
    {
        try
        {
            var layer = Plugin.HookLayers;
            var stats = layer.Parser.GetCacheMemoryStats();
            totalNativeBytes = stats.TotalNativeMemoryBytes;
            retiredNativeBytes = stats.RetiredNativeMemoryBytes;
            activeResourceCount = stats.ActiveResourceCount;
            retiredResourceCount = stats.RetiredResourceCount;
            retiredManagerCount = stats.RetiredManagerCount;

            columnCacheCount = layer.ColumnCacheCount;
            sheetSchemaCacheCount = layer.StringColumnCacheCount;
            sheetSchema = layer.GetStringColumnIndicesCacheSnapshot().OrderBy(entry => entry.Key, StringComparer.Ordinal)
                .Select(entry => (SheetName: entry.Key, StringColumnIndices: entry.Value)).ToArray();

            lastError = null;
        }
        catch (Exception ex)
        {
            lastError = ex.Message;
        }
        finally
        {
            lastLoadedAt = DateTime.Now;
        }
    }

    public void Draw()
    {
        if (!Ready)
        {
            ImGui.TextDisabled("Memory stats not readed.");
            if (ImGui.Button("Load##native_memory"))
                Load();
            return;
        }

        if (lastError is not null)
        {
            ImGui.TextColored(new Vector4(0.90f, 0.30f, 0.30f, 1f), $"Stats read error: {lastError}");
            ImGui.SameLine();
            if (ImGui.SmallButton("Retry##native_memory"))
                Refresh();
            return;
        }

        ImGui.TextDisabled($"Snapshot {lastLoadedAt:T}");
        ImGui.SameLine();
        if (ImGui.SmallButton("Refresh##native_memory"))
            Refresh();

        ImGui.Spacing();
        ImGui.TextUnformatted($"Translation caches: {FormatBytes(totalNativeBytes)} native payload");
        ImGui.TextDisabled($"    {activeResourceCount:N0} active, {retiredResourceCount:N0} retired resources in {retiredManagerCount:N0} manager(s)");

        ImGui.TextUnformatted($"Retained retired cache: {FormatBytes(retiredNativeBytes)}");
        ImGui.TextDisabled($"    {retiredResourceCount:N0} resources, {retiredManagerCount:N0} manager(s)");

        ImGui.TextUnformatted($"EXD column lookup cache: {columnCacheCount:N0} entries");
        ImGui.TextDisabled("    game column address -> sheet, row, and column metadata");

        ImGui.TextUnformatted($"EXD sheet schema cache: {sheetSchemaCacheCount:N0} entries");
        ImGui.TextDisabled("    game sheet name -> global indexes of string columns");

        ImGui.Spacing();
        ImGui.TextDisabled("Reported memory is unmanaged translation-buffer payload; object overhead is not included.");

        ImGui.Spacing();
        ImGui.Checkbox("Show sheet schema breakdown##native_memory", ref showSchemaBreakdown);
        if (!showSchemaBreakdown)
            return;
        
        ImGui.InputTextWithHint("##schema_filter", "Filter by sheet name...", ref filter, 128);
        
        var filtered = string.IsNullOrWhiteSpace(filter) ? sheetSchema : sheetSchema.Where(e => e.SheetName.Contains(filter, StringComparison.OrdinalIgnoreCase)).ToArray();
        
        if (!ImGui.BeginTable("##sheet_schema_table", 3, ImGuiTableFlags.Borders | ImGuiTableFlags.RowBg | ImGuiTableFlags.ScrollY, new Vector2(0, 260)))
            return;
        
        ImGui.TableSetupColumn("Sheet");
        ImGui.TableSetupColumn("String columns");
        ImGui.TableSetupColumn("Global indices");
        ImGui.TableHeadersRow();

        foreach (var entry in filtered)
        {
            ImGui.TableNextRow();
            ImGui.TableNextColumn();
            ImGui.TextUnformatted(entry.SheetName);
            ImGui.TableNextColumn();
            ImGui.TextUnformatted(entry.StringColumnIndices.Length.ToString());
            ImGui.TableNextColumn();
            ImGui.TextUnformatted(string.Join(", ", entry.StringColumnIndices));
        }

        ImGui.EndTable();
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static string FormatBytes(long bytes)
    {
        const double megabyte = 1024d * 1024d;
        return $"{bytes:N0} B / {bytes / megabyte:N2} MiB";
    }
}
