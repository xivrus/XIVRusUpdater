using Dalamud.Plugin.Services;
using System;
using System.Collections.Generic;
using System.Text;

namespace XIVRusUpdater.Utils.Memory;

public static unsafe class StoreRowResolver
{
    private const int StoreRowSlot = 3;
    private const int VtableSlotsChecked = 8;

    public static nint Resolve(ISigScanner scanner, string signature, string name)
    {
        var address = scanner.ScanText(signature);

        var text = scanner.Module.BaseAddress + (nint)scanner.TextSectionOffset;
        var textEnd = text + scanner.TextSectionSize;
        var rdata = (byte*)scanner.RDataSectionBase;
        var rdataSize = scanner.RDataSectionSize;

        var references = 0;
        for (long offset = StoreRowSlot * 8; offset + ((VtableSlotsChecked - StoreRowSlot) * 8) <= rdataSize; offset += 8)
        {
            if (*(nint*)(rdata + offset) != address)
                continue;

            references++;

            var vtable = (nint*)(rdata + offset - (StoreRowSlot * 8));
            var valid = true;
            for (var slot = 0; valid && slot < VtableSlotsChecked; slot++)
                valid = vtable[slot] >= text && vtable[slot] < textEnd;

            if (valid)
                return address;
        }

        throw new InvalidOperationException($"{name} at {address:X} is not slot {StoreRowSlot} of a row resolver vtable " + $"({references} references in .rdata, code {text:X}..{textEnd:X}).");
    }
}

