using System;
using System.Collections.Generic;
using System.IO;
using XIVRusUpdater.Core.Resource.Readers;
using XIVRusUpdater.Utils;

namespace XIVRusUpdater.Core.Resource;

public class FileResource : IDisposable
{
    // RowId -> String Columns Allocation
    public Dictionary<uint, List<ByteArrayWrapper?>> Rows { get; init; }

    private static readonly Dictionary<ResourceFormat, Func<IResourceFormatReader>> Readers = new()
    {
        [ResourceFormat.Xrt] = () => new XrtResourceFormatReader(),
        [ResourceFormat.Csv] = () => new CsvResourceFormatReader(),
    };

    public FileResource(string filePath, ResourceFormat format, string? sheetName = null)
    {
        if (!File.Exists(filePath))
            throw new FileNotFoundException(null, filePath);

        if (!Readers.TryGetValue(format, out var factory))
            throw new NotSupportedException($"Format '{format}' is not supported.");

        using var stream = File.OpenRead(filePath);
        Rows = factory().Read(stream, sheetName ?? Path.GetFileNameWithoutExtension(filePath));
    }

    public FileResource(string filePath, string format)
        : this(filePath, ResourceFormatParser.Parse(format)) { }

    public FileResource(string filePath)
        : this(filePath, ResourceFormatParser.FromExtension(filePath)) { }

    public bool TryGetData(uint rowId, uint column, out ByteArrayWrapper? value)
    {
        value = null;

        if (!Rows.TryGetValue(rowId, out var row))
            return false;

        if (column >= row.Count)
            return false;

        value = row[(int)column];
        return value is not null && !value.IsError;
    }

    public long GetNativeMemoryUsage()
    {
        long size = 0;
        foreach (var columns in Rows.Values)
        {
            foreach (var column in columns)
                size += column?.Length ?? 0;
        }

        return size;
    }

    public void Dispose()
    {
        DisposeRows(Rows);
        Rows.Clear();
    }

    public static void DisposeRows(Dictionary<uint, List<ByteArrayWrapper?>> rows)
    {
        foreach (var (_, columns) in rows)
        {
            DisposeColumns(columns);
        }
    }

    internal static void DisposeColumns(IEnumerable<ByteArrayWrapper?> columns)
    {
        foreach (var column in columns)
            column?.Dispose();
    }
}
