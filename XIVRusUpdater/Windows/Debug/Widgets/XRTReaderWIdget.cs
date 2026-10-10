using Dalamud.Bindings.ImGui;
using Dalamud.Interface.ImGuiFileDialog;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using XIVRusUpdater.Core.Resource;
using XIVRusUpdater.Utils;

namespace XIVRusUpdater.Windows.Debug.Widgets;

public class XRTReaderWIdget : IDebugWindowWidget
{
    public string[]? CommandShortcuts { get; init; } = ["xrt", "xrtreader"];
    public string DisplayName { get; init; } = "XRT Reader";
    public bool Ready { get; set; }

    private readonly FileDialogManager _fileDialog = new();

    private FileResource? _resource;

    public void Draw()
    {
        if (!Ready)
            return;

        DrawFileSelector();

        _fileDialog.Draw();

        if (_resource == null)
            return;

        DrawTable();
    }

    public void Load()
    {
        Ready = true;
    }

    private void DrawFileSelector()
    {
        if (ImGui.Button("Open XRT"))
        {
            _fileDialog.OpenFileDialog("Open XRT File",".xrt", OnFileSelected);
        }

    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private void OnFileSelected(bool success, string path)
    {
        if (!success || string.IsNullOrEmpty(path))
            return;

        _resource?.Dispose();

        _resource = new FileResource(path, ResourceFormat.Xrt);
    }

    private void DrawTable()
    {
        var rows = _resource!.Rows;

        var columnCount = rows.First().Value.Count();

        if (!ImGui.BeginTable("##XrtTable", columnCount + 1, ImGuiTableFlags.Borders | ImGuiTableFlags.RowBg | ImGuiTableFlags.Resizable | ImGuiTableFlags.ScrollX | ImGuiTableFlags.ScrollY))
        {
            return;
        }

        ImGui.TableSetupColumn("Row ID", ImGuiTableColumnFlags.WidthFixed, 80);

        for (var column = 0; column < columnCount; column++)
        {
            ImGui.TableSetupColumn($"Column {column}", ImGuiTableColumnFlags.WidthStretch);
        }

        ImGui.TableHeadersRow();

        foreach (var (rowId, columns) in rows)
        {
            ImGui.TableNextRow();

            ImGui.TableNextColumn();
            ImGui.TextUnformatted(rowId.ToString());

            for (var column = 0; column < columnCount; column++)
            {
                ImGui.TableNextColumn();

                if (column >= columns.Count)
                {
                    ImGui.TextDisabled("-");
                    continue;
                }

                var value = columns[column];

                ImGui.TextUnformatted(value!.Value);
            }
        }

        ImGui.EndTable();
    }
}
