using Dalamud.Bindings.ImGui;
using Dalamud.Interface.Utility.Raii;
using FFXIVClientStructs.FFXIV.Client.System.Framework;
using FFXIVClientStructs.FFXIV.Common.Component.Excel;
using FFXIVClientStructs.FFXIV.Component.Excel;
using FFXIVClientStructs.FFXIV.Component.Exd;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;

namespace XIVRusUpdater.Windows.Debug.Widgets;

public unsafe class RowMemoryWidget : IDebugWindowWidget
{
    public string[]? CommandShortcuts { get; init; } = ["row", "memory"];
    public string DisplayName { get; init; } = "Row Memory Inspector";
    public bool Ready { get; set; }

    private string _sheetName = string.Empty;
    private uint _rowId = 0;
    private ExcelSheet* _sheet;
    private ExcelRow* _row;
    private string? _loadedSheetName;
    private uint? _loadedRowId;
    private string? _error;
    private readonly List<ColumnEntry> _columns = [];

    private sealed class ColumnEntry
    {
        public int Index { get; init; }
        public ushort ColumnIndex { get; init; }
        public ushort Offset { get; init; }
        public ExcelColumnType Type { get; init; }
        public int Length { get; set; }
        public string Raw { get; set; } = string.Empty;
        public string Value { get; set; } = string.Empty;
        public string Address { get; set; } = string.Empty;
    }

    private bool IsDirty => !string.Equals(_sheetName.Trim(), _loadedSheetName, StringComparison.Ordinal) || _loadedRowId != _rowId;

    public void Load()
    {
        Ready = true;
    }

    public void Draw()
    {
        if (!Ready)
            return;

        ImGui.InputText("Sheet Name", ref _sheetName, 128);
        ImGui.InputUInt("Row ID", ref _rowId, 8);

        bool valid = !string.IsNullOrWhiteSpace(_sheetName);

        using (ImRaii.Disabled(!valid || !IsDirty))
        {
            if (ImGui.Button("Load"))
            {
                LoadRow(_sheetName.Trim(), (uint)_rowId);
            }
        }
        ImGui.SameLine();
        if (ImGui.Button("Clear"))
            Clear();

        if (!string.IsNullOrEmpty(_error))
            ImGui.TextWrapped(_error);

        if (_row == null || _sheet == null)
            return;

        ImGui.Text($"Sheet: {_loadedSheetName} | Row ID: {_loadedRowId} | Row Length: {_sheet->RowLength} | Columns: {_sheet->ColumnCount}");

        using var table = ImRaii.Table("RowColumns", 7, ImGuiTableFlags.Borders | ImGuiTableFlags.RowBg | ImGuiTableFlags.Resizable | ImGuiTableFlags.ScrollX | ImGuiTableFlags.ScrollY);
        if (table.Success)
        {
            ImGui.TableSetupColumn("#");
            ImGui.TableSetupColumn("Index");
            ImGui.TableSetupColumn("Offset");
            ImGui.TableSetupColumn("Type");
            ImGui.TableSetupColumn("Length");
            ImGui.TableSetupColumn("Raw Bytes");
            ImGui.TableSetupColumn("Value");
            ImGui.TableHeadersRow();

            foreach (ColumnEntry column in _columns)
            {
                ImGui.TableNextRow();
                ImGui.TableNextColumn();
                ImGui.Text(column.Index.ToString(CultureInfo.InvariantCulture));
                ImGui.TableNextColumn();
                ImGui.Text(column.ColumnIndex.ToString(CultureInfo.InvariantCulture));
                ImGui.TableNextColumn();
                ImGui.Text($"0x{column.Offset:X4}");
                ImGui.TableNextColumn();
                ImGui.Text(Enum.GetName(column.Type));
                ImGui.TableNextColumn();
                ImGui.Text(column.Length.ToString(CultureInfo.InvariantCulture));
                ImGui.TableNextColumn();
                ImGui.TextUnformatted(column.Raw);
                ImGui.TableNextColumn();
                ImGui.TextUnformatted(column.Value);
            }
        }
    }

    private unsafe bool LoadRow(string sheetName, uint rowId)
    {
        Clear();

        ExdModule* module = Framework.Instance()->ExdModule;
        ExcelSheet* sheet = module->GetSheetByName(sheetName);

        if (sheet == null || sheet->SheetIndex < 1 || sheet->Variant == ExcelVariant.MultiRow)
        {
            _error = $"Sheet '{sheetName}' not found or unsupported.";
            return false;
        }

        ExcelRow* row = module->GetRowBySheetAndRowId(sheet, rowId);

        if (row == null || row->Data == null)
        {
            _error = $"Row {rowId} not found in sheet '{sheetName}'.";
            return false;
        }

        _sheet = sheet;
        _row = row;
        _loadedSheetName = sheetName;
        _loadedRowId = rowId;

        BuildColumns();
        return true;
    }

    private void BuildColumns()
    {
        _columns.Clear();

        if (_sheet == null || _row == null || _row->Data == null || _sheet->ColumnDefinitions == null)
            return;

        var ordered = new List<ExcelSheet.ColumnInfo>(_sheet->ColumnDefinitionSpan.ToArray());
        ordered.Sort((a, b) => a.Offset.CompareTo(b.Offset));

        for (int i = 0; i < ordered.Count; i++)
        {
            ExcelSheet.ColumnInfo column = ordered[i];

            if (column.Offset >= _sheet->RowLength)
                continue;

            int nextOffset = i + 1 < ordered.Count ? ordered[i + 1].Offset : (int)_sheet->RowLength;
            int length = GetColumnSize(column.ColumnType);

            if (length == 0)
                length = nextOffset - column.Offset;

            length = Math.Min(length, (int)_sheet->RowLength - column.Offset);

            if (length <= 0)
                continue;

            void* columnPtr = (byte*)_row->Data + column.Offset;
            string raw = ReadHex(columnPtr, length);
            string value = DecodeValue(column.ColumnType, columnPtr, length);

            _columns.Add(new ColumnEntry
            {
                Index = i,
                ColumnIndex = column.Index,
                Offset = column.Offset,
                Type = column.ColumnType,
                Length = length,
                Raw = raw,
                Value = value
            });
        }
    }

    private static string ReadHex(void* data, int length)
    {
        if (data == null || length <= 0)
            return string.Empty;

        return Convert.ToHexString(new ReadOnlySpan<byte>(data, length));
    }

    // https://github.com/aers/FFXIVClientStructs/blob/main/ExcelGenerator/Util.cs

    private static unsafe string DecodeValue(ExcelColumnType type, void* data, int length)
    {
        if (data == null || length <= 0)
            return string.Empty;

        return type switch
        {
            ExcelColumnType.Bool when length >= 1 => (*(byte*)data != 0).ToString(),
            ExcelColumnType.Int8 when length >= 1 => (*(sbyte*)data).ToString(CultureInfo.InvariantCulture),
            ExcelColumnType.UInt8 when length >= 1 => (*(byte*)data).ToString(CultureInfo.InvariantCulture),
            ExcelColumnType.Int16 when length >= 2 => (*(short*)data).ToString(CultureInfo.InvariantCulture),
            ExcelColumnType.UInt16 when length >= 2 => (*(ushort*)data).ToString(CultureInfo.InvariantCulture),
            ExcelColumnType.Int32 when length >= 4 => (*(int*)data).ToString(CultureInfo.InvariantCulture),
            ExcelColumnType.UInt32 when length >= 4 => (*(uint*)data).ToString(CultureInfo.InvariantCulture),
            ExcelColumnType.Float32 when length >= 4 => (*(float*)data).ToString("G9", CultureInfo.InvariantCulture),
            ExcelColumnType.Int64 when length >= 8 => (*(long*)data).ToString(CultureInfo.InvariantCulture),
            ExcelColumnType.UInt64 when length >= 8 => (*(ulong*)data).ToString(CultureInfo.InvariantCulture),
            ExcelColumnType.String when length >= 4 => ReadExcelString(data),
            _ => ReadHex(data, length)
        };
    }

    private static int GetColumnSize(ExcelColumnType type) => type switch
    {
        ExcelColumnType.Bool => 1,
        ExcelColumnType.Int8 => 1,
        ExcelColumnType.UInt8 => 1,
        ExcelColumnType.Int16 => 2,
        ExcelColumnType.UInt16 => 2,
        ExcelColumnType.Int32 => 4,
        ExcelColumnType.UInt32 => 4,
        ExcelColumnType.Float32 => 4,
        ExcelColumnType.Int64 => 8,
        ExcelColumnType.UInt64 => 8,
        ExcelColumnType.String => 4,
        _ => 0
    };

    private static string GetColumnTypeName(ExcelColumnType type) => type.ToString();

    private static unsafe string ReadExcelString(void* columnPtr, int maxLength = 512)
    {
        if (columnPtr == null)
            return string.Empty;

        uint reference = *(uint*)columnPtr;
        uint offset = reference & 0x00FFFFFF;
        byte* ptr = (byte*)columnPtr + offset;

        int length = 0;
        while (length < maxLength && ptr[length] != 0)
            length++;

        string value = Encoding.UTF8.GetString(new ReadOnlySpan<byte>(ptr, length));
        return $"Offset: 0x{offset:X6}; Raw text: '{value}'";
    }

    private void Clear()
    {
        _sheet = null;
        _row = null;
        _loadedSheetName = null;
        _loadedRowId = null;
        _columns.Clear();
        _error = null;
    }
}
