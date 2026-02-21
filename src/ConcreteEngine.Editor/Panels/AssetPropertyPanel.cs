using System.Numerics;
using ConcreteEngine.Core.Common.Text;
using ConcreteEngine.Core.Engine.Assets;
using ConcreteEngine.Editor.Controller;
using ConcreteEngine.Editor.Controller.Proxy;
using ConcreteEngine.Editor.Core;
using ConcreteEngine.Editor.Data;
using ConcreteEngine.Editor.Panels.Assets;
using ConcreteEngine.Editor.UI;
using ConcreteEngine.Editor.UI.Widgets;
using ConcreteEngine.Editor.Utils;
using Hexa.NET.ImGui;

namespace ConcreteEngine.Editor.Panels;

internal sealed class AssetPropertyPanel(PanelContext context, AssetController assetController) : EditorPanel(PanelId.AssetProperty, context)
{
    private Popup _popup = new(new Vector2(12f, 10f));

    private readonly TexturePropertyUi _textureProxyUi = new(context);
    private readonly MaterialPropertyUi _materialProxyUi = new(context, assetController);

    public override void Enter()
    {
    }

    public override void Leave()
    {
    }

    public override void Draw(in FrameContext ctx)
    {
        if (Context.Selection.SelectedAsset is not { } editorAsset) return;

        DrawHeader(editorAsset, in ctx);

        var pos = new Vector2(ImGui.GetItemRectMin().X - 200, ImGui.GetItemRectMin().Y - 50);
        if (_popup.Begin("##asset-file-specs"u8, pos))
        {
            AssetGuiHelper.DrawFilesTable(editorAsset.FileSpecs, ctx.Writer);
            _popup.End();
        }

        switch (editorAsset)
        {
            case EditorShader shader:
                DrawShaderProperties(shader.Asset, in ctx);
                break;
            case EditorModel model:
                DrawModelProperties(model.Asset, in ctx);
                if (model.Asset.Animation is {} animation)
                    DrawAnimated(animation, ctx.Writer);
                break;
            case EditorTexture texture:
                _textureProxyUi.Draw(texture, in ctx);
                break;
            case EditorMaterial material:
                _materialProxyUi.DrawMaterialProperties(material, in ctx);
                break;
        }

    }

    private void DrawHeader(EditorAsset editorAsset, in FrameContext ctx)
    {
        var sw = ctx.Writer;
        var asset = editorAsset.Asset;

        if (ImGui.ArrowButton("<"u8, ImGuiDir.Left))
            _popup.State = true;

        ImGui.SameLine();
        ImGui.TextUnformatted(ref WriteFormat.WriteIdAndGen(sw, asset.Id, asset.Generation));
        ImGui.SameLine();
        ImGui.PushFont(null, 15);
        ImGui.TextColored(StyleMap.GetAssetColor(asset.Kind), ref sw.Write(asset.Name));
        ImGui.PopFont();
        ImGui.Separator();
    }


    private void DrawShaderProperties(Shader shader, in FrameContext ctx)
    {
        var layout = new TextLayout();
        ImGui.Spacing();

        // The Action Area
        if (ImGui.Button("Reload Shader"u8, new Vector2(-1, 0)))
            Context.EnqueueEvent(new AssetReloadEvent(shader.Name));

        if (ImGui.IsItemHovered())
            ImGui.SetTooltip("Recompiles source files."u8);

/*
        if (shaderProp.HasErrors)
        {
            ImGui.PushStyleColor(ImGuiCol.Text, new Vector4(1.0f, 0.4f, 0.4f, 1.0f));
            ImGui.TextWrapped("Last build failed. Check console for details."u8);
            ImGui.PopStyleColor();
        }
        */
    }

    private void DrawModelProperties(Model asset, in FrameContext ctx)
    {
        var sw = ctx.Writer;

        var info = asset.Info;
        var layout = new TextLayout()
            .TitleSeparator("Model Statistics"u8)
            .Property("Vertices:"u8, ref sw.Write(info.VertexCount))
            .Property("Triangles:"u8, ref sw.Write(info.FaceCount))
            .Property("Meshes:"u8, ref sw.Write((int)info.MeshCount))
            .Property("Animated:"u8, WriteFormat.BoolToYesNoShort(info.IsAnimated))
            .TitleSeparator("Meshes"u8);

        var meshes = asset.Meshes;
        foreach (var mesh in meshes)
        {
            if (!ImGui.TreeNodeEx(ref sw.Write(mesh.Name), ImGuiTreeNodeFlags.SpanFullWidth)) continue;

            var spec = mesh.Info;
            layout.Property("Index:"u8, ref sw.Write(spec.MeshIndex))
                .Property("MatIndex:"u8, ref sw.Write(spec.MaterialIndex))
                .Property("Vertices:"u8, ref sw.Write(spec.VertexCount))
                .Property("Triangles:"u8, ref sw.Write(spec.TrisCount));

            ImGui.TreePop();
        }
    }

    private void DrawAnimated(ModelAnimation animation, UnsafeSpanWriter sw)
    {
        
        var layout = new TextLayout()
            .TitleSeparator("Animation"u8)
            .Property("Bone Count:"u8, ref sw.Write(animation.BoneCount));

        if (!ImGui.BeginTable("##anim_table"u8, 4, GuiTheme.TableFlags)) return;

        layout.RowStretch("Name"u8).Row("Duration"u8, 50f).Row("TPS"u8, 50f).Row("Track"u8, 36f);
        ImGui.TableHeadersRow();

        layout.WithLayout(TextAlignMode.VerticalCenter);
        foreach (var clip in animation.Clips)
        {
            ImGui.TableNextRow();
            layout.Column(ref sw.Write(clip.Name)).Column(ref sw.Write(clip.Duration))
                .Column(ref sw.Write(clip.TicksPerSecond)).Column(ref sw.Write(clip.Length));
        }

        ImGui.EndTable();
       
    }
}