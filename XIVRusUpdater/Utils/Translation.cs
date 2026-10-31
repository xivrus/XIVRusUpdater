using System;
using System.Collections.Generic;
using System.Text;

namespace XIVRusUpdater.Utils;

public readonly struct Translation
{
    public readonly byte[]? Bytes;
    public readonly byte Hash;

    public Translation(byte[] bytes)
    {
        Bytes = bytes;
        Hash = ExcelStringHash.Compute(bytes);
    }

    public bool IsValid => Bytes is not null;
}

// Component::Excel::ExcelRow_CalculateStringColumnHash
internal static class ExcelStringHash
{
    public static byte Compute(ReadOnlySpan<byte> text)
    {
        byte hash = 0;
        foreach (var b in text)
            hash = (byte)(b ^ (hash << 1));
        return hash;
    }
}
