using System.Numerics;
using ConcreteEngine.Core.Engine.Assets;
using ConcreteEngine.Editor.Bridge;
using ConcreteEngine.Editor.Core;
using ConcreteEngine.Editor.Data;
using ConcreteEngine.Editor.UI;
using ConcreteEngine.Editor.Utils;
using Hexa.NET.ImGui;

namespace ConcreteEngine.Editor.Components.Assets;

internal sealed class AssetBaseUi(AssetsComponent component)
{
    private Popup _popup = new(new Vector2(12f, 10f));

    public void Draw(AssetState state, AssetProxy proxy, ref FrameContext ctx)
    {
        var asset = proxy.Asset;
        var fileSpecs = proxy.FileSpecs;
        ref var sw = ref ctx.Sw;

        if (ImGui.ArrowButton("<"u8, ImGuiDir.Left))
            _popup.State = true;

        TextLayout.Make()
            .TitleWithId(ref sw, asset.Kind.ToTextUtf8(), asset.Id)
            .PropertyColor(asset.Kind.ToColor(), "Name:"u8, sw.Write(asset.Name))
            .Property("Gen:"u8, sw.Write(asset.Generation));
        

        var pos = new Vector2(ImGui.GetItemRectMin().X - 256, ImGui.GetItemRectMin().Y);
        if (_popup.Begin(state.GetPopupId(), pos))
        {
            DrawFilesTable(fileSpecs, ref sw);
            _popup.End();
        }
    }

    private void DrawFilesTable(AssetFileSpec[] fileSpecs, ref SpanWriter sw)
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
            layout.NextColumn(sw.Write(it.Id.Value));
            layout.NextColumn(sw.Write(it.RelativePath));
            layout.NextColumn(sw.Write(it.SizeBytes));
            layout.NextColumn(sw.Write(it.ContentHash ?? ""));
            ImGui.PopID();
        }


        ImGui.EndTable();
    }
}