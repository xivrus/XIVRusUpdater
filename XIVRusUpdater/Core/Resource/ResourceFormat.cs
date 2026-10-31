using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace XIVRusUpdater.Core.Resource;

public enum ResourceFormat
{
    Xrt,
    Csv
}

public static class ResourceFormatParser
{
    private static readonly Dictionary<string, ResourceFormat> Aliases = new(StringComparer.OrdinalIgnoreCase)
    {
        [".xrt"] = ResourceFormat.Xrt,
        ["xrt"] = ResourceFormat.Xrt,
        [".csv"] = ResourceFormat.Csv,
        ["csv"] = ResourceFormat.Csv,
    };

    private static readonly Dictionary<ResourceFormat, string> Extensions = new()
    {
        [ResourceFormat.Xrt] = "xrt",
        [ResourceFormat.Csv] = "csv",
    };

    public static ResourceFormat Parse(string text)
    {
        if (Aliases.TryGetValue(text, out var byAlias))
            return byAlias;

        if (Enum.TryParse<ResourceFormat>(text, ignoreCase: true, out var byName))
            return byName;

        throw new ArgumentException($"Unknown resource format: '{text}'", nameof(text));
    }

    public static ResourceFormat FromExtension(string filePath) => Parse(Path.GetExtension(filePath));

    public static string GetExtension(ResourceFormat format)
    {
        if (!Extensions.TryGetValue(format, out var ext))
            throw new NotSupportedException($"Format '{format}' has no registered extension.");

        return ext;
    }
}
