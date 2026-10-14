using Dalamud.Bindings.ImGui;
using Dalamud.Interface.Utility;
using Dalamud.Interface.Utility.Raii;
using Dalamud.Interface.Windowing;
using System;
using System.Numerics;

namespace XIVRusUpdater.Windows;

public sealed class ChangelogWindow : Window, IDisposable
{
    private readonly Plugin _plugin;

    public ChangelogWindow(Plugin plugin)
        : base("XIV Rus Update Changelog###XIVRusChangelog")
    {
        _plugin = plugin;
        RespectCloseHotkey = false;
        RespectCloseHotkey = false;

        SizeConstraints = new WindowSizeConstraints
        {
            MinimumSize = new Vector2(375, 330),
            MaximumSize = new Vector2(float.MaxValue, float.MaxValue),
            
        };
        Size = new Vector2(750, 600) * ImGuiHelpers.GlobalScale;

        Flags = ImGuiWindowFlags.NoCollapse;
        SizeCondition = ImGuiCond.FirstUseEver;

    }

    public void Dispose()
    {
    }

    public override void Draw()
    {
        ImGui.TextUnformatted("Translation has been updated.");
        ImGui.Separator();

        var contentHeight = ImGui.GetContentRegionAvail().Y - 50 * ImGuiHelpers.GlobalScale;

        using(var child = ImRaii.Child("##changelog", new Vector2(0, contentHeight), true))
        {
            if(child.Success)
                ImGui.TextWrapped(Plugin.State.LastChangelog ?? "No changelog available.");
        }

        ImGui.Spacing();

        if (ImGui.Button("Accept"))
        {
            Plugin.State.Penumbra.ShowChangelog = false;
            Plugin.State.Translation.ShowChangelog = false;
        }
    }
}
