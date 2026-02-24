using System.Numerics;
using ConcreteEngine.Core.Common.Text;
using ConcreteEngine.Core.Engine.Assets;
using ConcreteEngine.Editor.Bridge;
using ConcreteEngine.Editor.Core;
using ConcreteEngine.Editor.Data;
using ConcreteEngine.Editor.Panels.Inspector;
using ConcreteEngine.Editor.UI;
using ConcreteEngine.Editor.UI.Widgets;
using ConcreteEngine.Editor.Utils;
using Hexa.NET.ImGui;

namespace ConcreteEngine.Editor.Panels;

internal sealed class AssetPropertyPanel(PanelContext context, AssetController assetController)
    : EditorPanel(PanelId.AssetProperty, context)
{
    private readonly TextureInspectorUi _textureProxyUi = new(context, assetController);
    private readonly MaterialPropertyUi _materialProxyUi = new(context, assetController);
    private readonly ShaderInspectorUi _shaderInspectorUi = new(context, assetController);
    private readonly ModelInspectorUi _modelInspectorUi = new(context, assetController);

    private Popup _popup = new(new Vector2(12f, 10f));

    public override void Draw(in FrameContext ctx)
    {
        if (Context.Selection.SelectedAsset is not { } editorAsset) return;
        
        ImGui.PushID(editorAsset.Asset.Id);
        DrawHeader(editorAsset, ctx);
        ImGui.Spacing();
        ImGui.Separator();
        
        switch (editorAsset)
        {
            case EditorShader shader:
                _shaderInspectorUi.Draw(shader, in ctx);
                break;
            case EditorModel model:
                _modelInspectorUi.Draw(model, in ctx);
                break;
            case EditorTexture texture:
                _textureProxyUi.Draw(texture, in ctx);
                break;
            case EditorMaterial material:
                _materialProxyUi.Draw(material, in ctx);
                break;
        }
        ImGui.PopID();
    }

    private void DrawHeader(EditorAsset editorAsset, FrameContext ctx)
    {
        var asset = editorAsset.Asset;

        if (ImGui.ArrowButton("<"u8, ImGuiDir.Left))
            _popup.State = true;

        ImGui.SameLine();
        ImGui.TextUnformatted(ref WriteFormat.WriteIdAndGen(ctx.Sw, asset.Id, asset.Generation));
        ImGui.SameLine();
        ImGui.PushFont(null, 15);
        ImGui.TextColored(StyleMap.GetAssetColor(asset.Kind), ref ctx.Sw.Write(asset.Name));
        ImGui.PopFont();
        
        //info popup
        var pos = new Vector2(ImGui.GetItemRectMin().X - 200, ImGui.GetItemRectMin().Y - 50);
        if (_popup.Begin("##asset-file-specs"u8, pos))
        {
            DrawFilesTable(editorAsset.FileSpecs, ctx.Sw);
            _popup.End();
        }
    }

    private static void DrawFilesTable(AssetFileSpec[] fileSpecs, UnsafeSpanWriter sw)
    {
        ImGui.SeparatorText("Files"u8);
        if (!ImGui.BeginTable("##asset_store_files_tbl"u8, 4, ImGuiTableFlags.Borders)) return;

        var layout = new TableLayout()
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