using Dalamud.Bindings.ImGui;
using Dalamud.Interface.Windowing;
using System;
using System.Numerics;
using XIVRusUpdater.Utils;

namespace XIVRusUpdater.Windows;

public sealed class ChangelogWindow : Window, IDisposable
{
    private readonly Plugin plugin;

    public ChangelogWindow(Plugin plugin)
        : base("XIV Rus Update Changelog###XIVRusChangelog")
    {
        Flags = ImGuiWindowFlags.NoCollapse;
        RespectCloseHotkey = false;
        this.plugin = plugin;

        Size = new Vector2(750, 600);
        SizeCondition = ImGuiCond.FirstUseEver;

        RespectCloseHotkey = false;
    }

    public void Dispose()
    {
    }

    public override void Draw()
    {
        ImGui.TextUnformatted("XIV Rus has been updated.");
        ImGui.Separator();

        var contentHeight = ImGui.GetContentRegionAvail().Y - 50;

        ImGui.BeginChild("##changelog", new Vector2(0, contentHeight), true);

        string markdown = Plugin.State.LastChangelog ?? "No changelog available.";

        ImGui.TextWrapped(markdown);

        ImGui.EndChild();

        ImGui.Spacing();

        if (ImGui.Button("Accept", new Vector2(180, 0)))
        {
            Plugin.State.Penumbra.ShowChangelog = false;
            Plugin.State.Translation.ShowChangelog = false;
        }
    }
}
