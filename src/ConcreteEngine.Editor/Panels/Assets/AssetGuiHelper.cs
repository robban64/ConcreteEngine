using ConcreteEngine.Core.Engine.Assets;
using ConcreteEngine.Editor.UI;
using ConcreteEngine.Editor.Utils;
using Hexa.NET.ImGui;

namespace ConcreteEngine.Editor.Panels.Assets;

internal static class AssetGuiHelper
{
    public static void DrawFilesTable(AssetFileSpec[] fileSpecs, ref SpanWriter sw)
    {
        ImGui.SeparatorText("Files"u8);
        if (!ImGui.BeginTable("##asset_store_files_tbl"u8, 4, ImGuiTableFlags.Borders)) return;

        var layout = new TextLayout()
            .Row("ID"u8).RowStretch("Path"u8).Row("Size"u8).Row("Hash"u8);

        ImGui.TableHeadersRow();
        foreach (var it in fileSpecs)
        {
            ImGui.PushID(it.Id.Value);
            ImGui.TableNextRow();
            layout.Column(sw.Write(it.Id.Value));
            layout.Column(sw.Write(it.RelativePath));
            layout.Column(sw.Write(it.SizeBytes));
            layout.Column(sw.Write(it.ContentHash ?? ""));
            ImGui.PopID();
        }
        ImGui.EndTable();
    }
}