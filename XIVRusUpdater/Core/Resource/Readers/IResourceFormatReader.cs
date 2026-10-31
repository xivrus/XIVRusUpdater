using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using XIVRusUpdater.Utils;

namespace XIVRusUpdater.Core.Resource.Readers;

public interface IResourceFormatReader
{
    public Dictionary<uint, Translation[]> Read(Stream stream, string sheetName);
}
