// Based on Harmonia (https://github.com/AngelicaProject/Harmonia), Copyright (C) 2026 pokeda.
// Modified for XIVRusUpdater on 2026-10-09. Licensed under AGPL-3.0-or-later.

using FFXIVClientStructs.FFXIV.Component.Exd;

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
