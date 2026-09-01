using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace XIVRusUpdater.Utils.Extentions;

public static class ByteReaderExtensions
{
    extension(BinaryReader reader)
    {
        public byte[] ReadStringNullterminated()  
        {
            List<byte> result = [];

            byte currentByte;

            while ((currentByte = reader.ReadByte()) != 0x00)
            {
                result.Add(currentByte);
            }
            result.Add((byte) 0x00);

            return [.. result];
        }
    }
}
