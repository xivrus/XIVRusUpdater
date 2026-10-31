using FFXIVClientStructs.FFXIV.Component.Exd;
using System;
using System.Collections.Generic;
using System.Text;

namespace XIVRusUpdater.Utils.Memory;

public sealed unsafe class RowAllocator(nint environmentPointer)
{
    private readonly ExdEnvironment** instance = (ExdEnvironment**)environmentPointer;

    public byte* Alloc(int size)
    {
        var environment = *instance;
        if (environment == null)
            return null;

        var vtable = *(nint**)environment;
        var alloc = (delegate* unmanaged<ExdEnvironment*, ulong, ulong, byte*>)vtable[1];
        return alloc(environment, (ulong)size, 0);
    }

    public void Free(byte* buffer)
    {
        var environment = *instance;
        if (environment == null)
            return;

        var vtable = *(nint**)environment;
        var free = (delegate* unmanaged<ExdEnvironment*, void*, void*>)vtable[2];
        free(environment, buffer);
    }
}
