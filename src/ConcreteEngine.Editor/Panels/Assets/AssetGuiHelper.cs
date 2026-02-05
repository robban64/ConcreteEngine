using ConcreteEngine.Core.Common.Text;
using ConcreteEngine.Core.Engine.Assets;
using ConcreteEngine.Editor.UI;
using Hexa.NET.ImGui;

namespace ConcreteEngine.Editor.Panels.Assets;

internal static class AssetGuiHelper
{
    public static void DrawFilesTable(AssetFileSpec[] fileSpecs, UnsafeSpanWriter sw)
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
            layout.Column(ref sw.Write(it.Id.Value));
            layout.Column(ref sw.Write(it.RelativePath));
            layout.Column(ref sw.Write(it.SizeBytes));
            layout.Column(ref sw.Write(it.ContentHash ?? ""));
            ImGui.PopID();
        }

        ImGui.EndTable();
    }
}