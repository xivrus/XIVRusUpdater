using System;
using System.Collections.Generic;
using System.IO;
using Dalamud.Interface.ImGuiNotification;
using Lumina.Text.Parse;
using Lumina.Text.ReadOnly;
using nietras.SeparatedValues;
using XIVRusUpdater.Utils;

namespace XIVRusUpdater.Core.Resource.Readers;

public sealed class CsvResourceFormatReader : IResourceFormatReader
{
    private static readonly MacroStringParseOptions MacroStringOptions = new(default);

    public Dictionary<uint, List<ByteArrayWrapper?>> Read(Stream stream, string sheetName)
    {
        var rows = new Dictionary<uint, List<ByteArrayWrapper?>>();

        try
        {
            using var reader = Sep.Reader(o => o with { HasHeader = false }).From(stream);

            List<int>? stringColumnIndices = null;
            var failedRowCount = 0;

            foreach (var row in reader)
            {
                if (row.ColCount == 0)
                {
                    continue;
                }

                var firstCol = row[0].Span;

                if (IsServiceRow(firstCol))
                {
                    continue;
                }

                if (firstCol is "Int32")
                {
                    stringColumnIndices = GetStringColumnIndices(row);
                    continue;
                }

                if (!TryGetRowId(firstCol, stringColumnIndices, out var parsedRowId))
                {
                    continue;
                }

                // columns[i] всегда соответствует i-й строковой колонке из заголовка CSV, даже если значение отсутствует или не удалось распарсить.
                var columns = new List<ByteArrayWrapper?>(stringColumnIndices!.Count);
                try
                {
                    var rowFailed = ParseStringColumns(row, stringColumnIndices, parsedRowId, sheetName, columns);

                    if (!TryAddRow(rows, (uint)parsedRowId, columns))
                        continue;

                    if (rowFailed)
                        failedRowCount++;
                }
                catch
                {
                    FileResource.DisposeColumns(columns);
                    throw;
                }
            }

            NotifyParseFailures(sheetName, failedRowCount);

            return rows;
        }
        catch
        {
            FileResource.DisposeRows(rows);
            throw;
        }
    }

    private static bool IsServiceRow(ReadOnlySpan<char> firstColumn) =>
        firstColumn is "key" ||
        firstColumn is "#" ||
        firstColumn is "offset";

    private static bool TryGetRowId(
        ReadOnlySpan<char> firstColumn,
        List<int>? stringColumnIndices,
        out int rowId)
    {
        rowId = 0;

        return stringColumnIndices is not null &&
               int.TryParse(firstColumn, out rowId) &&
               rowId >= 0;
    }

    private static List<int> GetStringColumnIndices(SepReader.Row row)
    {
        var indices = new List<int>();

        for (var i = 0; i < row.ColCount; i++)
        {
            if (row[i].Span is "String")
                indices.Add(i);
        }

        return indices;
    }

    private static bool ParseStringColumns(
        SepReader.Row row,
        List<int> stringColumnIndices,
        int rowId,
        string sheetName,
        List<ByteArrayWrapper?> columns)
    {
        var rowFailed = false;

        foreach (var colIdx in stringColumnIndices)
        {
            if (colIdx >= row.ColCount)
            {
                columns.Add(null);
                continue;
            }

            var textSpan = row[colIdx].Span.Trim('"');

            if (TryParseMacroString(textSpan, out var bytes, out var error))
            {
                columns.Add(new ByteArrayWrapper(bytes));
                continue;
            }

            rowFailed = true;
            Plugin.Log?.Error(
                $"[CSV] ({sheetName}) Failed to parse row {rowId}, column {colIdx}: {error}");
            columns.Add(new ByteArrayWrapper($"Row {rowId}, column {colIdx}: {error}"));
        }

        return rowFailed;
    }

    private static bool TryAddRow(
        Dictionary<uint, List<ByteArrayWrapper?>> rows,
        uint rowId,
        List<ByteArrayWrapper?> columns)
    {
        if (!rows.TryAdd(rowId, columns))
        {
            FileResource.DisposeColumns(columns);
            return false;
        }

        return true;
    }

    private static void NotifyParseFailures(string sheetName, int failedRowCount)
    {
        if (failedRowCount == 0 || !Plugin.Instance.Configuration.ShowNotifications)
            return;

        Plugin.NotificationManager.AddNotification(new Notification
        {
            Type = NotificationType.Warning,
            Content = $"CSV ({sheetName}): failed to parse {failedRowCount} row(s).",
        });
    }

    private static bool TryParseMacroString(ReadOnlySpan<char> input, out byte[] bytes, out string error)
    {
        // экранирование через \\ не поддерживается
        if (input.IndexOf(@"\\,".AsSpan()) >= 0)
        {
            bytes = [];
            error = "Double-escaped comma in macro string; use a single backslash before the comma.";
            return false;
        }

        try
        {
            ReadOnlySpan<byte> encoded = ReadOnlySeString.FromMacroString(input, MacroStringOptions).AsSpan();
            bytes = [.. encoded];
            if (bytes.Length == 0 || bytes[^1] != 0)
                bytes = [.. bytes, 0];

            error = string.Empty;
            return true;
        }
        catch (Exception exception)
        {
            bytes = [];
            error = exception.Message;
            return false;
        }
    }
}
