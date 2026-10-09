using Dalamud.Hooking;
using Dalamud.Plugin.Services;
using FFXIVClientStructs.FFXIV.Common.Component.Excel;
using FFXIVClientStructs.FFXIV.Component.Excel;
using Serilog;
using System;
using System.Runtime.CompilerServices;
using XIVRusUpdater.Core;
using XIVRusUpdater.Utils.Memory;

namespace XIVRusUpdater.Hooks;

public unsafe partial class EXDHooks : IDisposable
{
    public const string HashTableStoreRowSig = "48 89 5C 24 08 48 89 74 24 10 57 48 83 EC 20 44 8B 49 18 33 C0 49 8B F0 48 8B FA 45 85 C9";

    public const string RingBufferStoreRowSig = "48 89 5C 24 08 48 89 6C 24 10 48 89 74 24 18 48 89 7C 24 20 41 54 41 56 41 57 48 83 EC 20 48 8B 59 20 48 8D 41 28";

    public const string ExdEnvironmentInstanceSig = "48 8B 0D ?? ?? ?? ?? 48 8B 7A";

    private const int MaxStringColumns = 512;

    public readonly TranslationParser parser;

    private readonly TranslationGate gate;
    private readonly RowAllocator allocator;
    private readonly SheetNameCache sheetNames = new();
    private readonly Hook<StoreRowDelegate> hashTableHook;
    private readonly Hook<StoreRowDelegate> ringBufferHook;
    private bool disposed;

    public nint HashTableAddress { get; }
    public nint RingBufferAddress { get; }

    private delegate byte StoreRowDelegate(nint resolver, ExcelRowDescriptor* descriptor, ExcelRow* row);

    public EXDHooks(IGameInteropProvider provider, ISigScanner scanner, string engineId)
    {
        parser = new TranslationParser(engineId);
        gate = new TranslationGate(parser);
        allocator = new RowAllocator(scanner.GetStaticAddressFromSig(ExdEnvironmentInstanceSig));

        var hashTable = StoreRowResolver.Resolve(scanner, HashTableStoreRowSig, "HashTableExcelPageRowResolver::StoreRow");
        var ringBuffer = StoreRowResolver.Resolve(scanner, RingBufferStoreRowSig, "RingBufferExcelPageRowResolver::StoreRow");

        hashTableHook = provider.HookFromAddress<StoreRowDelegate>(hashTable, HashTableDetour);
        ringBufferHook = provider.HookFromAddress<StoreRowDelegate>(ringBuffer, RingBufferDetour);

        EnableAll();

        HashTableAddress = hashTable;
        RingBufferAddress = ringBuffer;

    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void UpdateEngine(string engineId) => parser.UpdateEngine(engineId);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void EnableAll()
    {
        hashTableHook.Enable();
        ringBufferHook.Enable();
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void DisableAll()
    {
        hashTableHook.Disable();
        ringBufferHook.Disable();
    }

    public void Dispose()
    {
        if (disposed)
            return;

        disposed = true;
        DisableAll();
        
        hashTableHook.Dispose();
        ringBufferHook.Dispose();
        parser.Dispose();
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private void DisposeHooks()
    {
        hashTableHook.Dispose();
        ringBufferHook.Dispose();
    }

    private byte HashTableDetour(nint resolver, ExcelRowDescriptor* descriptor, ExcelRow* row)
    {
        Translate(descriptor, row);
        return hashTableHook.Original(resolver, descriptor, row);
    }

    private byte RingBufferDetour(nint resolver, ExcelRowDescriptor* descriptor, ExcelRow* row)
    {
        Translate(descriptor, row);
        return ringBufferHook.Original(resolver, descriptor, row);
    }

    private void Translate(ExcelRowDescriptor* descriptor, ExcelRow* row)
    {
        if (descriptor == null || row == null)
            return;

        try
        {
            TranslateRow(descriptor, row);
        }
        catch (Exception ex)
        {
            Log.Debug(ex, "[EXDHooks] row translation failed");
        }
    }

    private void TranslateRow(ExcelRowDescriptor* descriptor, ExcelRow* row)
    {
        var sheet = row->Sheet;
        var data = (byte*)row->Data;
        if (sheet == null || data == null || sheet->Version <= 2)
            return;

        var stringCount = StringColumns.Count(sheet);
        if (stringCount == 0 || stringCount > MaxStringColumns)
            return;

        var sheetName = sheetNames.Get(sheet);
        if (!gate.AllowsRow(sheetName, descriptor->RowId))
            return;

        if (sheet->Variant == ExcelVariant.MultiRow && descriptor->SubRowCount != 1)
            return;

        Span<ExcelStringField> fields = stackalloc ExcelStringField[stringCount];
        StringColumns.Fill(sheet, fields);

        Span<ExcelStringSpan> spans = stackalloc ExcelStringSpan[stringCount];
        var dataOffset = (int)sheet->DataOffset;
        if (!ExcelRowRewriter.TryLocate(data, dataOffset, fields, spans))
            return;

        Span<StringPatch> patches = stackalloc StringPatch[stringCount];
        patches.Clear();

        var any = false;
        for (var ordinal = 0; ordinal < stringCount; ordinal++)
            any |= gate.TryGetPatch(sheetName, descriptor->RowId, ordinal, out patches[ordinal]);

        if (!any)
            return;

        var size = ExcelRowRewriter.ComputeSize(dataOffset, spans, patches);
        if (size < 0)
            return;

        var target = allocator.Alloc(size);
        if (target == null)
            return;

        ExcelRowRewriter.Rebuild(data, target, dataOffset, spans, patches);
        row->Data = target;
        allocator.Free(data);
    }
}
