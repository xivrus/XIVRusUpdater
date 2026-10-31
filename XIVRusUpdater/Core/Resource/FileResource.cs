using System;
using System.Collections.Generic;
using System.IO;
using XIVRusUpdater.Core.Resource.Readers;
using XIVRusUpdater.Utils;

namespace XIVRusUpdater.Core.Resource;

public class FileResource
{
    // RowId -> String Columns Allocation
    public Dictionary<uint, Translation[]> Rows { get; init; }

    private static readonly Dictionary<ResourceFormat, Func<IResourceFormatReader>> Readers = new()
    {
        [ResourceFormat.Xrt] = static() => new XrtResourceFormatReader(),
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

    internal bool TryGetData(uint rowId, uint column, out Translation? value)
    {
        value = null;

        if (!Rows.TryGetValue(rowId, out var row))
            return false;

        if (column >= row.Length)
            return false;

        value = row[(int)column];
        return value is not null;
    }

    internal bool TryGetData(uint rowId, out Translation[]? columns)
    {
        columns = null;
        if (Rows.TryGetValue(rowId, out var row))
        {
            columns = row;
            return true;
        }

        return false;
    }
}
