using Dalamud.Bindings.ImGui;
using System;
using System.Collections.Generic;
using System.Text;

namespace XIVRusUpdater.Windows.Debug;

// https://github.com/goatcorp/Dalamud/blob/master/Dalamud/Interface/Internal/Windows/Data/WidgetUtil.cs
internal class WidgetUtil
{
    internal static void DrawCopyableText(string text, string tooltipText = "Copy")
    {
        ImGui.TextWrapped(text);

        if (ImGui.IsItemHovered())
        {
            ImGui.SetMouseCursor(ImGuiMouseCursor.Hand);
            ImGui.BeginTooltip();
            ImGui.Text(tooltipText);
            ImGui.EndTooltip();
        }

        if (ImGui.IsItemClicked())
        {
            ImGui.SetClipboardText(text);
        }
    }
}
