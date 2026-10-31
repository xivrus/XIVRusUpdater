using System;
using System.Collections.Generic;
using System.Text;
using XIVRusUpdater.Utils.Memory;

namespace XIVRusUpdater.Core;

public sealed unsafe class TranslationGate(TranslationParser parser)
{
    public bool AllowsRow(string? sheetName, uint rowId) => !string.IsNullOrEmpty(sheetName) && parser.IsSheetLoaded(sheetName) && Plugin.filter.IsActive(sheetName, rowId);

    public bool TryGetPatch(string sheetName, uint rowId, int ordinal, out StringPatch patch)
    {
        patch = default;

        if (!parser.TryGetValue(sheetName, rowId, (uint)ordinal, out var translation) || translation is null || translation.IsError || translation.Pointer == null)
            return false;

        patch = new StringPatch(translation.Pointer, translation.Length);
        return true;
    }
}
