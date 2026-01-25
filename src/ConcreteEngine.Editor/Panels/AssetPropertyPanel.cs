using System.Numerics;
using ConcreteEngine.Editor.Core;
using ConcreteEngine.Editor.Core.Definitions;
using ConcreteEngine.Editor.Panels.Assets;
using ConcreteEngine.Editor.Proxy;
using ConcreteEngine.Editor.UI;
using ConcreteEngine.Editor.Utils;
using Hexa.NET.ImGui;

namespace ConcreteEngine.Editor.Panels;

internal sealed class AssetPropertyPanel(PanelContext context) : EditorPanel(PanelId.AssetProperty, context)
{
    private Popup _popup = new(new Vector2(12f, 10f));

    private readonly TexturePropertyUi _textureProxyUi = new();
    private readonly MaterialPropertyUi _materialProxyUi = new();

    public override void Leave()
    {
    }

    public override void Draw(in FrameContext ctx)
    {
        var proxy = Context.AssetProxy;
        if (proxy is null) return;
        if (!ImGui.BeginChild("##asset-props"u8, ImGuiChildFlags.AlwaysUseWindowPadding)) return;

        var asset = proxy.Asset;
        var fileSpecs = proxy.FileSpecs;
        var kind = asset.Kind;

        var sw = ctx.Writer;

        if (ImGui.ArrowButton("<"u8, ImGuiDir.Left))
            _popup.State = true;

        ImGui.SameLine();
        ImGui.TextUnformatted(ref WriteFormat.WriteIdAndGen(sw, asset.Id, asset.Generation));
        ImGui.SameLine();
        ImGui.PushFont(null, 15);
        ImGui.TextColored(kind.ToColor(), ref sw.Write(asset.Name));
        ImGui.PopFont();
        ImGui.Separator();

        var pos = new Vector2(ImGui.GetItemRectMin().X - 200, ImGui.GetItemRectMin().Y - 50);
        if (_popup.Begin("##asset-file-specs"u8, pos))
        {
            AssetGuiHelper.DrawFilesTable(fileSpecs, sw);
            _popup.End();
        }

        switch (proxy.Property)
        {
            case ShaderProxyProperty shaderProp:
                DrawShaderProperties(proxy, shaderProp, in ctx);
                break;
            case ModelProxyProperty modelProxy:
                DrawModelProperties(modelProxy, in ctx);
                if (modelProxy.Asset.IsAnimated)
                    DrawAnimated(modelProxy, sw);
                break;
            case TextureProxyProperty texProp:
                _textureProxyUi.Draw(texProp, in ctx);
                break;
            case MaterialProxyProperty matProp:
                _materialProxyUi.DrawMaterialProperties(matProp, in ctx);
                break;
        }

        ImGui.EndChild();
    }


    private void DrawShaderProperties(AssetObjectProxy proxy, ShaderProxyProperty prop, in FrameContext ctx)
    {
        var layout = new TextLayout();
        ImGui.Spacing();

        // The Action Area
        if (ImGui.Button("Reload Shader"u8, new Vector2(-1, 0))) ;
        //TriggerEvent(new AssetEvent(EventKey.SelectionAction, proxy.Asset.Id) { Name = proxy.Asset.Name });

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

    public void DrawModelProperties(ModelProxyProperty prop, in FrameContext ctx)
    {
        var asset = prop.Asset;
        var sw = ctx.Writer;

        var layout = new TextLayout().TitleSeparator("Model Statistics"u8)
            .Property("Total Tris:"u8, ref sw.Write(asset.DrawCount))
            .Property("Mesh Count:"u8, ref sw.Write(asset.MeshCount))
            .Property("Animated:"u8, WriteFormat.BoolToYesNoShort(asset.IsAnimated))
            .TitleSeparator("Mesh Parts"u8);

        var meshes = prop.Meshes;
        foreach (var mesh in meshes)
        {
            if (!ImGui.TreeNodeEx(ref sw.Write(mesh.Name), ImGuiTreeNodeFlags.SpanFullWidth)) continue;

            var spec = mesh.Spec;
            layout.Property("Index:"u8, ref sw.Write(spec.MeshIndex))
                .Property("Material ID:"u8, ref sw.Write(spec.MaterialIndex))
                .Property("Tris:"u8, ref sw.Write(spec.DrawCount));

            ImGui.TreePop();
        }

    }

    private void DrawAnimated(ModelProxyProperty prop, StrWriter8 sw)
    {
        var layout = new TextLayout()
            .TitleSeparator("Animation"u8)
            .Property("Bone Count:"u8, ref sw.Write(prop.BoneCount));
        
        if (!ImGui.BeginTable("##anim_table"u8, 4, GuiTheme.TableFlags)) return;

        layout.RowStretch("Name"u8).Row("Duration"u8,50f).Row("TPS"u8,50f).Row("Track"u8,36f);
        ImGui.TableHeadersRow();

        layout.WithLayout(TextAlignMode.VerticalCenter);
        foreach (var clip in prop.Clips)
        {
            ImGui.TableNextRow();
            layout.Column(ref sw.Write(clip.Name)).Column(ref sw.Write(clip.Duration))
                .Column(ref sw.Write(clip.TicksPerSecond)).Column(ref sw.Write(clip.TrackCount));
        }

        ImGui.EndTable();
    }
}