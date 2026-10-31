using FFXIVClientStructs.FFXIV.Common.Component.Excel;
using System;
using System.Collections.Concurrent;

namespace XIVRusUpdater.Utils.Memory;

public sealed unsafe class SheetNameCache
{
    private readonly ConcurrentDictionary<nint, (nint NamePtr, string Name)> cache = new();

    public string Get(ExcelSheet* sheet)
    {
        var namePtr = (nint)sheet->SheetName.Value;
        if (cache.TryGetValue((nint)sheet, out var cached) && cached.NamePtr == namePtr)
            return cached.Name;

        var name = sheet->SheetName.ToString();
        cache[(nint)sheet] = (namePtr, name);
        return name;
    }
}

public static unsafe class StringColumns
{
    public static int Count(ExcelSheet* sheet)
    {
        var definitions = sheet->ColumnDefinitions;
        if (definitions == null)
            return 0;

        var count = 0;
        for (var i = 0; i < sheet->ColumnCount; i++)
        {
            if (definitions[i].Type == 0)
                count++;
        }

        return count;
    }

    public static void Fill(ExcelSheet* sheet, Span<ExcelStringField> fields)
    {
        var definitions = sheet->ColumnDefinitions;
        for (int i = 0, n = 0; i < sheet->ColumnCount; i++)
        {
            if (definitions[i].Type == 0)
                fields[n++] = new ExcelStringField((ushort)i, definitions[i].Offset);
        }
    }
}
