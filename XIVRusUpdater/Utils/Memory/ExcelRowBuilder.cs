using FFXIVClientStructs.FFXIV.Common.Component.Environment;
using FFXIVClientStructs.FFXIV.Common.Component.Excel;
using FFXIVClientStructs.FFXIV.Component.Excel;
using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using XIVRusUpdater.Utils;
using XIVRusUpdater.Utils.Extentions;
using XIVRusUpdater.Utils.Memory;

namespace XIVRusUpdater.Core;

internal static unsafe class ExcelRowBuilder
{
    public const uint OffsetMask = 0x00FFFFFF;
    public const int MaxRowBytes = 1 << 20;
    private const int StackLimit = 128;

    // See Component::Excel::ExcelRow_Parse_v3 as reference
    public static byte* Build(ExcelRow* row, StringColumnLayout layout, ReadOnlySpan<Translation> translations, AllocatorInterface* allocator)
    {
        var source = (byte*)row->Data;
        var offsets = layout.StringOffsets;
        var dataOffset = layout.DataOffset;
        var count = offsets.Length;

        Span<int> lengths = count <= StackLimit ? stackalloc int[count] : new int[count];

        var prevStart = 0;
        for (var i = 0; i < count; i++)
        {
            var fieldOffset = offsets[i];
            var start = fieldOffset + (int)(*(uint*)(source + fieldOffset) & OffsetMask);

            if ((uint)start >= MaxRowBytes)
                return null;

            if (i == 0)
            {
                if (start != dataOffset)
                    return null;
            }
            else
            {
                var len = start - prevStart - 1;
                if (len < 0 || source[start - 1] != 0)
                    return null;

                lengths[i - 1] = len;
            }

            prevStart = start;
        }

        var lastLength = MemoryMarshal.CreateReadOnlySpanFromNullTerminated(source + prevStart).Length;
        if (prevStart + lastLength >= MaxRowBytes)
            return null;
        lengths[count - 1] = lastLength;

        long totalSize = dataOffset + count;
        for (var i = 0; i < count; i++)
            totalSize += (i < translations.Length ? translations[i].Bytes?.Length : null) ?? lengths[i];

        if (totalSize > MaxRowBytes)
            return null;

        var target = allocator->MemAlloc<byte>((ulong)totalSize);
        if (target == null)
            return null;

        new ReadOnlySpan<byte>(source, dataOffset).CopyTo(new Span<byte>(target, dataOffset));

        var srcCursor = dataOffset;
        var dstCursor = dataOffset;

        for (var i = 0; i < count; i++)
        {
            var fieldOffset = offsets[i];
            var originalLength = lengths[i];
            var translation = i < translations.Length ? translations[i] : default;

            int length;
            byte hash;

            if (translation.Bytes is { } bytes)
            {
                length = bytes.Length;
                hash = translation.Hash;
                bytes.CopyTo(new Span<byte>(target + dstCursor, length));
            }
            else
            {
                length = originalLength;
                hash = (byte)(*(uint*)(source + fieldOffset) >> 24);
                new ReadOnlySpan<byte>(source + srcCursor, length)
                    .CopyTo(new Span<byte>(target + dstCursor, length));
            }

            target[dstCursor + length] = 0;
            *(uint*)(target + fieldOffset) = ((uint)hash << 24) | (uint)(dstCursor - fieldOffset);

            srcCursor += originalLength + 1;
            dstCursor += length + 1;
        }

        return target;
    }
}
