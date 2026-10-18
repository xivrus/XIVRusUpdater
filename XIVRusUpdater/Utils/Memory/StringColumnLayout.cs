using FFXIVClientStructs.FFXIV.Common.Component.Excel;
using FFXIVClientStructs.FFXIV.Component.Excel;
using System;
using System.Collections.Generic;
using System.Text;
using XIVRusUpdater.Core;

namespace XIVRusUpdater.Utils.Memory;

internal sealed unsafe class StringColumnLayout
{
    public int DataOffset { get; }
    public int[] StringOffsets { get; }

    private StringColumnLayout(int dataOffset, int[] offsets)
    {
        DataOffset = dataOffset;
        StringOffsets = offsets;
    }

    public static StringColumnLayout? Create(ExcelRow* row)
    {
        var sheet = row->Sheet;
        if (sheet == null || sheet->ColumnDefinitions == null)
            return null;

        var dataOffset = (int)sheet->DataOffset;
        if (dataOffset <= 0 || dataOffset > ExcelRowBuilder.MaxRowBytes)
            return null;

        var offsets = new List<int>();
        for (var i = 0; i < sheet->ColumnCount; i++)
        {
            var column = sheet->ColumnDefinitions[i];
            if (column.ColumnType != ExcelColumnType.String)
                continue;

            var offset = (int)column.Offset;
            if (offset < 0 || offset + sizeof(uint) > dataOffset)
                return null;

            offsets.Add(offset);
        }

        return offsets.Count == 0 ? null : new StringColumnLayout(dataOffset, offsets.ToArray());
    }
}
