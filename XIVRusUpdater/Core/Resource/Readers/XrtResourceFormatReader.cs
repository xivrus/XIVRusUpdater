using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using XIVRusUpdater.Core.Resource;
using XIVRusUpdater.Utils;
using XIVRusUpdater.Utils.Extentions;

namespace XIVRusUpdater.Core.Resource.Readers;

public sealed class XrtResourceFormatReader : IResourceFormatReader
{
    private static readonly byte[] Magic = "XRT\0"u8.ToArray();

    public Dictionary<uint, Translation[]> Read(Stream stream, string sheetName)
    {
        var rows = new Dictionary<uint, Translation[]>();

        using var reader = new BinaryReader(stream);

        var magic = reader.ReadBytes(Magic.Length);

        if (!magic.AsSpan().SequenceEqual(Magic))
            throw new InvalidDataException("Invalid XRT format.");

        ushort version = reader.ReadUInt16();

        if (version != 1)
            throw new InvalidDataException($"Unsupported XRT version: {version}");

        ushort stringColumnCount = reader.ReadUInt16();

        while (stream.Position < stream.Length)
        {
            long remain = stream.Length - stream.Position;

            if (remain < sizeof(uint))
                throw new InvalidDataException("Incomplete XRT row header.");

            uint rowId = reader.ReadUInt32();

            var columns = new List<Translation>(stringColumnCount);

            for (int i = 0; i < stringColumnCount; i++)
            {
                byte[] value = reader.ReadStringNullterminated();
                columns.Add(new Translation(value));
            }

            if (rows.ContainsKey(rowId))
                continue;

            rows.Add(rowId, columns.ToArray());
                
        }

        return rows;
    }
}
