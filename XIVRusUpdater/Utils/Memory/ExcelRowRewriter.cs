using System;
using System.Collections.Generic;
using System.Text;

namespace XIVRusUpdater.Utils.Memory;

public readonly record struct ExcelStringField(ushort Index, ushort Offset);

public readonly record struct ExcelStringSpan(int FieldOffset, int Start, int Length);

public readonly unsafe struct StringPatch(byte* text, int length)
{
    public byte* Text { get; } = text;
    public int Length { get; } = length;
    public bool IsSet => Text != null;
}

public static unsafe class ExcelRowRewriter
{
    private const uint OffsetMask = 0xFFFFFF;
    private const int MaxRowBytes = 1 << 20;

    public static bool TryLocate(byte* data, int dataOffset, ReadOnlySpan<ExcelStringField> fields, Span<ExcelStringSpan> spans)
    {
        if (data == null || dataOffset <= 0 || dataOffset > MaxRowBytes || spans.Length < fields.Length)
            return false;

        var cursor = dataOffset;
        for (var i = 0; i < fields.Length; i++)
        {
            var field = fields[i].Offset;
            if (field + 4 > dataOffset)
                return false;
            if (field + (*(uint*)(data + field) & OffsetMask) != cursor)
                return false;

            var length = 0;
            while (data[cursor + length] != 0)
            {
                if (++length + cursor >= MaxRowBytes)
                    return false;
            }

            spans[i] = new ExcelStringSpan(field, cursor, length);
            cursor += length + 1;
        }

        return true;
    }

    public static int ComputeSize(int dataOffset, ReadOnlySpan<ExcelStringSpan> spans, ReadOnlySpan<StringPatch> patches)
    {
        long size = dataOffset;
        for (var i = 0; i < spans.Length; i++)
            size += (patches[i].IsSet ? patches[i].Length : spans[i].Length) + 1;

        return size > MaxRowBytes ? -1 : (int)size;
    }

    public static void Rebuild(byte* source, byte* target, int dataOffset, ReadOnlySpan<ExcelStringSpan> spans, ReadOnlySpan<StringPatch> patches)
    {
        Buffer.MemoryCopy(source, target, dataOffset, dataOffset);
        var cursor = dataOffset;
        for (var i = 0; i < spans.Length; i++)
        {
            var span = spans[i];
            var patch = patches[i];
            var text = patch.IsSet ? patch.Text : source + span.Start;
            var length = patch.IsSet ? patch.Length : span.Length;

            Buffer.MemoryCopy(text, target + cursor, length, length);
            target[cursor + length] = 0;

            var field = target + span.FieldOffset;
            *(uint*)field = (uint)(cursor - span.FieldOffset);
            field[3] = patch.IsSet ? Hash(text, length) : source[span.FieldOffset + 3];
            cursor += length + 1;
        }
    }

    private static byte Hash(byte* text, int length)
    {
        byte hash = 0;
        for (var i = 0; i < length; i++)
            hash = (byte)(text[i] ^ (hash << 1));
        return hash;
    }
}
