using Dalamud.Bindings.ImGui;
using Dalamud.Interface;
using Dalamud.Interface.Components;
using Dalamud.Interface.Utility;
using Dalamud.Interface.Windowing;
using Dalamud.Utility;
using Serilog;
using System;
using System.Linq;
using System.Numerics;
using XIVRusUpdater.Windows.Debug.Widgets;

namespace XIVRusUpdater.Windows.Debug;

public class DebugWindow : Window, IDisposable
{
    private readonly IDebugWindowWidget[] modules =
    [
        new XRTReaderWIdget(),
        new SheetCacheCoverageWidget(),
        new NativeMemoryUsageWidget(),
    ];

    private readonly IOrderedEnumerable<IDebugWindowWidget> orderedModules;

    private bool isExcept;
    private bool selectionCollapsed;

    private bool isLoaded;

    public DebugWindow()
        : base("XIVRus Debug", ImGuiWindowFlags.NoScrollbar | ImGuiWindowFlags.NoScrollWithMouse)
    {
        Size = new Vector2(400, 300);
        SizeCondition = ImGuiCond.FirstUseEver;

        RespectCloseHotkey = false;
        orderedModules = modules.OrderBy(module => module.DisplayName);
        CurrentWidget = orderedModules.First();
    }

    public IDebugWindowWidget CurrentWidget { get; set; }

    public void Dispose() => this.modules.OfType<IDisposable>().AggregateToDisposable().Dispose();

    public override void OnOpen()
    {
        Load();
    }

    public override void OnClose()
    {
    }

    public T GetWidget<T>() where T : IDebugWindowWidget
    {
        foreach (var m in modules)
        {
            if (m is T w)
                return w;
        }

        throw new ArgumentException($"No widget of type {typeof(T).FullName} found.");
    }

    public void SetDataKind(string dataKind)
    {
        if (string.IsNullOrEmpty(dataKind))
            return;

        if (modules.FirstOrDefault(module => module.IsWidgetCommand(dataKind)) is { } targetModule)
        {
            CurrentWidget = targetModule;
        }
        else
        {
            Plugin.Log.Error($"/xivrus debug: Invalid data type {dataKind}");
        }
    }

    public override void Draw()
    {
        if (this.selectionCollapsed)
        {
            this.DrawContents();
            return;
        }

        if (ImGui.BeginTable("Debug_Table"u8, 2, ImGuiTableFlags.BordersInnerV | ImGuiTableFlags.Resizable))
        {
            ImGui.TableSetupColumn("##SelectionColumn"u8, ImGuiTableColumnFlags.WidthFixed, 200.0f * ImGuiHelpers.GlobalScale);
            ImGui.TableSetupColumn("##ContentsColumn"u8, ImGuiTableColumnFlags.WidthStretch);

            ImGui.TableNextColumn();
            DrawSelection();

            ImGui.TableNextColumn();
            DrawContents();

            ImGui.EndTable();
        }
    }

    private void DrawSelection()
    {
        if (ImGui.BeginChild("Debug_SelectionPane"u8, ImGui.GetContentRegionAvail()))
        {
            if (ImGui.BeginListBox("WidgetSelectionListbox"u8, ImGui.GetContentRegionAvail()))
            {
                foreach (var widget in orderedModules)
                {
                    if (ImGui.Selectable(widget.DisplayName, CurrentWidget == widget))
                    {
                        CurrentWidget = widget;
                    }
                }

                ImGui.EndListBox();
            }
        }

        ImGui.EndChild();
    }

    private void DrawContents()
    {
        if (ImGui.BeginChild("Debug_ContentsPanel"u8, ImGui.GetContentRegionAvail()))
        {
            if (ImGuiComponents.IconButton("collapse-expand", selectionCollapsed ? FontAwesomeIcon.ArrowRight : FontAwesomeIcon.ArrowLeft))
            {
                selectionCollapsed = !selectionCollapsed;
            }

            if (ImGui.IsItemHovered())
            {
                ImGui.SetTooltip($"{(selectionCollapsed ? "Expand"u8 : "Collapse"u8)} selection panel");
            }

            ImGui.SameLine();

            if (ImGuiComponents.IconButton("forceReload", FontAwesomeIcon.Sync))
            {
                isLoaded = false;
                Load();
            }

            if (ImGui.IsItemHovered())
            {
                ImGui.SetTooltip("Force Reload"u8);
            }

            ImGui.SameLine();

            var copy = ImGuiComponents.IconButton("copyAll", FontAwesomeIcon.ClipboardList);

            ImGuiHelpers.ScaledDummy(10.0f);

            if (ImGui.BeginChild("Debug_WidgetContents"u8, ImGui.GetContentRegionAvail()))
            {
                if (copy)
                    ImGui.LogToClipboard();

                try
                {
                    if (CurrentWidget is { Ready: true })
                    {
                        CurrentWidget.Draw();
                    }
                    else
                    {
                        ImGui.Text("Data not ready."u8);
                    }

                    isExcept = false;
                }
                catch (Exception ex)
                {
                    if (!isExcept)
                    {
                        Log.Error(ex, "Could not draw data");
                    }

                    isExcept = true;

                    ImGui.Text(ex.ToString());
                }
            }

            ImGui.EndChild();
        }

        ImGui.EndChild();
    }

    private void Load()
    {
        if (isLoaded)
            return;

        isLoaded = true;

        foreach (var widget in modules)
        {
            widget.Load();
        }
    }
}
