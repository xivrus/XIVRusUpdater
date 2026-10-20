using Dalamud.Hooking;
using Dalamud.Plugin.Services;
using FFXIVClientStructs.FFXIV.Client.System.Framework;
using FFXIVClientStructs.FFXIV.Common.Component.Environment;
using FFXIVClientStructs.FFXIV.Common.Component.Excel;
using FFXIVClientStructs.FFXIV.Component.Excel;
using System;
using System.Collections.Concurrent;
using System.Runtime.CompilerServices;
using XIVRusUpdater.Core;
using XIVRusUpdater.Utils.Memory;
using static ImSharp.Im.Native.Methods;

namespace XIVRusUpdater.Hooks;

public unsafe partial class EXDHooks : IDisposable
{
    private const string HashTableStoreRowSig = "48 89 5C 24 ?? 48 89 74 24 ?? 57 48 83 EC ?? 44 8B 49 ?? 33 C0 49 8B F0";
    private const string RingBufferStoreRowSig = "48 89 5C 24 ?? 48 89 6C 24 ?? 48 89 74 24 ?? 48 89 7C 24 ?? 41 54 41 56 41 57 48 83 EC ?? 48 8B 59 ?? 48 8D 41";

    public readonly TranslationParser parser;
    private readonly ConcurrentDictionary<string, StringColumnLayout?> layouts = new();

    private readonly Hook<StoreRowDelegate> hashTableHook;
    private readonly Hook<StoreRowDelegate> ringBufferHook;
    private delegate byte StoreRowDelegate(nint resolver, ExcelRowDescriptor* descriptor, ExcelRow* row);

    public EXDHooks(IGameInteropProvider provider, ISigScanner scanner, string engineId)
    {
        parser = new TranslationParser(engineId);

        var hashTable = scanner.ScanText(HashTableStoreRowSig);
        var ringBuffer = scanner.ScanText(RingBufferStoreRowSig);

        hashTableHook = provider.HookFromAddress<StoreRowDelegate>(scanner.ScanText(HashTableStoreRowSig), HashTableDetour);
        ringBufferHook = provider.HookFromAddress<StoreRowDelegate>(scanner.ScanText(RingBufferStoreRowSig), RingBufferDetour);

        EnableAll();
    }

    public void EnableAll()
    {
        hashTableHook.Enable();
        ringBufferHook.Enable();
    }

    public void DisableAll()
    {
        hashTableHook.Disable();
        ringBufferHook.Disable();
    }

    public void Dispose()
    {
        DisableAll();
        hashTableHook.Dispose();
        ringBufferHook.Dispose();
    }

    private byte HashTableDetour(nint resolver, ExcelRowDescriptor* descriptor, ExcelRow* row)
    {
        TryReplaceRow(row, descriptor);
        return hashTableHook.Original(resolver, descriptor, row);
    }

    private byte RingBufferDetour(nint resolver, ExcelRowDescriptor* descriptor, ExcelRow* row)
    {
        TryReplaceRow(row, descriptor);
        return ringBufferHook.Original(resolver, descriptor, row);
    }

    private bool TryReplaceRow(ExcelRow* row, ExcelRowDescriptor* descriptor)
    {
        if (row == null || descriptor == null)
            return false;

        var sheet = row->Sheet;
        if (sheet == null || row->Data == null)
            return false;

        if (sheet->Variant != ExcelVariant.SingleRow || sheet->Version <= 2)
            return false;

        var sheetName = sheet->SheetNameVar.SheetNamePtr.ToString();
        var rowId = descriptor->RowId;

        if (!parser.IsNeedTranslate(sheetName, rowId))
            return false;

        if (!parser.TryGetRowTranslations(sheetName, rowId, out var translations))
            return false;

        var layout = layouts.GetOrAdd(sheetName, static (_, r) => StringColumnLayout.Create((ExcelRow*)r), (nint)row);
        if (layout == null)
            return false;

        var allocator = (AllocatorInterface*)Framework.Instance()->ExdModule->ExdEnvironment;
        if (allocator == null)
            return false;

        var newData = ExcelRowBuilder.Build(row, layout, translations, allocator);
        if (newData == null)
            return false;

        row->Data = newData;
        return true;
    }
}
