using Dalamud.Hooking;
using Dalamud.Plugin.Services;
using FFXIVClientStructs.FFXIV.Component.Excel;
using System;
using System.Runtime.CompilerServices;
using XIVRusUpdater.Core;

namespace XIVRusUpdater.Hooks;

public unsafe partial class EXDHooks : IDisposable
{
    public const string HashTableStoreRowSig = "48 89 5C 24 ?? 48 89 74 24 ?? 57 48 83 EC ?? 44 8B 49 ?? 33 C0 49 8B F0";

    public const string RingBufferStoreRowSig = "48 89 5C 24 ?? 48 89 6C 24 ?? 48 89 74 24 ?? 48 89 7C 24 ?? 41 54 41 56 41 57 48 83 EC ?? 48 8B 59 ?? 48 8D 41";

    public readonly TranslationParser parser;

    private readonly TranslationGate gate;
    private readonly Hook<StoreRowDelegate> hashTableHook;
    private readonly Hook<StoreRowDelegate> ringBufferHook;
    private delegate byte StoreRowDelegate(nint resolver, ExcelRowDescriptor* descriptor, ExcelRow* row);

    public EXDHooks(IGameInteropProvider provider, ISigScanner scanner, string engineId)
    {
        parser = new TranslationParser(engineId);
        gate = new TranslationGate(parser);
        
        var hashTable = scanner.ScanText(HashTableStoreRowSig);
        var ringBuffer = scanner.ScanText(RingBufferStoreRowSig);

        hashTableHook = provider.HookFromAddress<StoreRowDelegate>(hashTable, HashTableDetour);
        ringBufferHook = provider.HookFromAddress<StoreRowDelegate>(ringBuffer, RingBufferDetour);

        EnableAll();
    }

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
        DisableAll();
        DisposeHooks();
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private void DisposeHooks()
    {
        hashTableHook.Dispose();
        ringBufferHook.Dispose();
    }

    private byte HashTableDetour(nint resolver, ExcelRowDescriptor* descriptor, ExcelRow* row)
    {
        return hashTableHook.Original(resolver, descriptor, row);
    }

    private byte RingBufferDetour(nint resolver, ExcelRowDescriptor* descriptor, ExcelRow* row)
    {
        return ringBufferHook.Original(resolver, descriptor, row);
    }
}
