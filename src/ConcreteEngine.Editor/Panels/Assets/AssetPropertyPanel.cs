using System.Numerics;
using ConcreteEngine.Core.Common.Text;
using ConcreteEngine.Core.Diagnostics.Time;
using ConcreteEngine.Editor.Controller.Proxy;
using ConcreteEngine.Editor.Core;
using ConcreteEngine.Editor.Core.Definitions;
using ConcreteEngine.Editor.UI;
using ConcreteEngine.Editor.UI.Widgets;
using ConcreteEngine.Editor.Utils;
using Hexa.NET.ImGui;

namespace ConcreteEngine.Editor.Panels.Assets;

internal sealed class AssetPropertyPanel(PanelContext context) : EditorPanel(PanelId.AssetProperty, context)
{
    private Popup _popup = new(new Vector2(12f, 10f));

    private readonly TexturePropertyUi _textureProxyUi = new();
    private readonly MaterialPropertyUi _materialProxyUi = new();

    public override void Enter()
    {
    }

    public override void Leave()
    {
    }

    public override void Draw(in FrameContext ctx)
    {
        if (Context.AssetProxy is not { } proxy) return;

        ImGui.BeginChild("asset-props"u8, ImGuiChildFlags.AlwaysUseWindowPadding);

        DrawHeader(proxy, in ctx);

        var pos = new Vector2(ImGui.GetItemRectMin().X - 200, ImGui.GetItemRectMin().Y - 50);
        if (_popup.Begin("##asset-file-specs"u8, pos))
        {
            AssetGuiHelper.DrawFilesTable(proxy.FileSpecs, ctx.Writer);
            _popup.End();
        }

        switch (proxy.Property)
        {
            case ShaderProxyProperty shaderProp:
                DrawShaderProperties(proxy, shaderProp, in ctx);
                break;
            case ModelProxyProperty modelProxy:
                DurationProfileTimer.Default.Begin();
                DrawModelProperties(modelProxy, in ctx);
                if (modelProxy.Asset.Info.IsAnimated)
                    DrawAnimated(modelProxy, ctx.Writer);
                DurationProfileTimer.Default.EndPrintSimple();
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

    private void DrawHeader(AssetObjectProxy proxy, in FrameContext ctx)
    {
        var asset = proxy.Asset;
        var sw = ctx.Writer;

        if (ImGui.ArrowButton("<"u8, ImGuiDir.Left))
            _popup.State = true;

        ImGui.SameLine();
        ImGui.TextUnformatted(ref WriteFormat.WriteIdAndGen(sw, asset.Id, asset.Generation));
        ImGui.SameLine();
        ImGui.PushFont(null, 15);
        ImGui.TextColored(asset.Kind.ToColor(), ref sw.Write(asset.Name));
        ImGui.PopFont();
        ImGui.Separator();
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

    private void DrawModelProperties(ModelProxyProperty prop, in FrameContext ctx)
    {
        prop.Draw(in ctx);
        /*
        var asset = prop.Asset;
        var sw = ctx.Writer;

        var info = asset.Info;
        var layout = new TextLayout()
            .TitleSeparator("Model Statistics"u8)
            .Property("Vertices:"u8, ref sw.Write(info.VertexCount))
            .Property("Triangles:"u8, ref sw.Write(info.FaceCount))
            .Property("Meshes:"u8, ref sw.Write((int)info.MeshCount))
            .Property("Animated:"u8, WriteFormat.BoolToYesNoShort(info.IsAnimated))
            .TitleSeparator("Meshes"u8);

        var meshes = prop.Meshes;
        foreach (var mesh in meshes)
        {
            if (!ImGui.TreeNodeEx(ref sw.Write(mesh.Name), ImGuiTreeNodeFlags.SpanFullWidth)) continue;

            var spec = mesh.Info;
            layout.Property("Index:"u8, ref sw.Write(spec.MeshIndex))
                .Property("MatIndex:"u8, ref sw.Write(spec.MaterialIndex))
                .Property("Vertices:"u8, ref sw.Write(spec.VertexCount))
                .Property("Triangles:"u8, ref sw.Write(spec.TrisCount));

            ImGui.TreePop();
        }*/
    }

    private void DrawAnimated(ModelProxyProperty prop, UnsafeSpanWriter sw)
    {
        /*
        var layout = new TextLayout()
            .TitleSeparator("Animation"u8)
            .Property("Bone Count:"u8, ref sw.Write(prop.BoneCount));

        if (!ImGui.BeginTable("##anim_table"u8, 4, GuiTheme.TableFlags)) return;

        layout.RowStretch("Name"u8).Row("Duration"u8, 50f).Row("TPS"u8, 50f).Row("Track"u8, 36f);
        ImGui.TableHeadersRow();

        layout.WithLayout(TextAlignMode.VerticalCenter);
        foreach (var clip in prop.Clips)
        {
            ImGui.TableNextRow();
            layout.Column(ref sw.Write(clip.Name)).Column(ref sw.Write(clip.Duration))
                .Column(ref sw.Write(clip.TicksPerSecond)).Column(ref sw.Write(clip.TrackCount));
        }

        ImGui.EndTable();
        */
    }
}