using System.Numerics;
using ConcreteEngine.Core.Engine.Assets;
using ConcreteEngine.Editor.Bridge;
using ConcreteEngine.Editor.Components.Assets;
using ConcreteEngine.Editor.Components.State;
using ConcreteEngine.Editor.Core;
using ConcreteEngine.Editor.Data;
using ConcreteEngine.Editor.Definitions;
using ConcreteEngine.Editor.UI;
using ConcreteEngine.Editor.Utils;
using Hexa.NET.ImGui;

namespace ConcreteEngine.Editor.Components;

internal sealed class AssetsComponent : EditorComponent
{
    private readonly AssetListUi _assetListUi;
    private readonly AssetBaseUi _assetBaseUi;

    private readonly TexturePropertyUi _textureProxyUi;
    private readonly MaterialPropertyUi _materialProxyUi;

    private readonly ClipDrawer<AssetProxy> _clipDrawer;
    public readonly AssetState State = new();

    public AssetsComponent()
    {
        _assetListUi = new AssetListUi(this);
        _materialProxyUi = new MaterialPropertyUi(this);
        _assetBaseUi = new AssetBaseUi(this);
        _textureProxyUi = new TexturePropertyUi(this);

        _clipDrawer = new ClipDrawer<AssetProxy>(_assetListUi.DrawListItem);
    }

    public void TriggerSelection(AssetId id)
        => TriggerEvent(new AssetEvent(EventKey.SelectionChanged, id));

    public void TriggerTextureUpdate(TextureProxyProperty prop, string name, int value) =>
        TriggerEvent(new AssetEvent(EventKey.SelectionUpdated, default));

    public override void DrawLeft(ref FrameContext ctx)
    {
        ImGui.SeparatorText("Asset Store"u8);

        _assetListUi.DrawTypeSelector(State, ref ctx);

        if (State.SelectAssetKind == AssetKind.Unknown) return;
        if (!ImGui.BeginTable("##asset_store_object_tbl"u8, 3, GuiTheme.TableFlags)) return;

        ImGui.TableSetupColumn("Type"u8, AssetListUi.ColumnWidth);
        ImGui.TableSetupColumn("Id"u8, AssetListUi.ColumnWidth);
        ImGui.TableSetupColumn("Name"u8, ImGuiTableColumnFlags.WidthStretch);
        ImGui.TableHeadersRow();

        var len = EngineController.AssetController.GetAssetSpan(State.SelectAssetKind).Length;
        _clipDrawer.Draw(len, AssetListUi.PaddedRowHeight, ctx.StateCtx.Selection.AssetProxy!, ref ctx);

        ImGui.EndTable();
    }

    public override void DrawRight(ref FrameContext ctx)
    {
        var proxy = ctx.StateCtx.Selection.AssetProxy;
        if (proxy is null) return;
        if (!ImGui.BeginChild("##asset-sidebar-props"u8, ImGuiChildFlags.AlwaysUseWindowPadding))
            return;

        _assetBaseUi.Draw(State, proxy, ref ctx);

        switch (proxy.Property)
        {
            case ShaderProxyProperty shaderProp:
                DrawShaderProperties(proxy, shaderProp, ref ctx);
                break;
            case ModelProxyProperty modelProxy:
                DrawModelProperties(modelProxy, ref ctx);
                break;
            case TextureProxyProperty texProp:
                _textureProxyUi.Draw(texProp, ref ctx);
                break;
            case MaterialProxyProperty matProp:
                _materialProxyUi.DrawMaterialProperties(matProp, ref ctx);
                break;
        }

        ImGui.EndChild();
    }


    public void DrawShaderProperties(AssetProxy proxy, ShaderProxyProperty prop, ref FrameContext ctx)
    {
        var layout = new TextLayout();
        ImGui.Spacing();

        // The Action Area
        if (ImGui.Button("Reload Shader"u8, new Vector2(-1, 0)))
            TriggerEvent(new AssetEvent(EventKey.SelectionAction, proxy.Asset.Id) { Name = proxy.Asset.Name });

        if (ImGui.IsItemHovered())
            ImGui.SetTooltip(ctx.Sw.Write("Recompiles source files."));

/*
        if (shaderProp.HasErrors)
        {
            ImGui.PushStyleColor(ImGuiCol.Text, new Vector4(1.0f, 0.4f, 0.4f, 1.0f));
            ImGui.TextWrapped("Last build failed. Check console for details."u8);
            ImGui.PopStyleColor();
        }
        */
    }

    public void DrawModelProperties(ModelProxyProperty prop, ref FrameContext ctx)
    {
        var asset = prop.Asset;
        ref var sw = ref ctx.Sw;
        var layout = new TextLayout();

        layout.TitleSeparator("Model Statistics"u8)
            .Property("Total Tris:"u8, sw.Write(asset.DrawCount))
            .Property("Mesh Count:"u8, sw.Write(asset.MeshCount))
            .Property("Animated:"u8, StrUtils.BoolToYesNoShort(asset.IsAnimated))
            .TitleSeparator("Mesh Parts"u8);

        var meshes = prop.Meshes;
        foreach (var mesh in meshes)
        {
            if (!ImGui.TreeNodeEx(sw.Write(mesh.Name), ImGuiTreeNodeFlags.SpanFullWidth)) continue;

            var spec = mesh.Spec;
            layout.Property("Index:"u8, sw.Write(spec.MeshIndex))
                .Property("Material ID:"u8, sw.Write(spec.MaterialIndex))
                .Property("Tris:"u8, sw.Write(spec.DrawCount));

            ImGui.TreePop();
        }

        if (asset.IsAnimated)
            DrawAnimated(prop, ref sw);
    }

    private void DrawAnimated(ModelProxyProperty prop, ref SpanWriter sw)
    {
        const ImGuiTableFlags flags =
            ImGuiTableFlags.NoPadOuterX | ImGuiTableFlags.NoBordersInBody | ImGuiTableFlags.SizingFixedFit;

        var layout = new TextLayout()
            .Property("Bone Count:"u8, sw.Write(prop.BoneCount))
            .TitleSeparator("Animation Clips"u8);

        if (!ImGui.BeginTable("##anim_table"u8, 4, flags)) return;

        layout.Row("Name"u8).Row("Duration"u8).Row("TPS"u8).Row("Track"u8);
        ImGui.TableHeadersRow();

        layout.WithLayout(TextAlignMode.VerticalCenter);
        foreach (var clip in prop.Clips)
        {
            ImGui.TableNextRow();
            layout.Column(sw.Write(clip.Name)).Column(sw.Write(clip.Duration)).Column(sw.Write(clip.TicksPerSecond));
        }

        ImGui.EndTable();
    }
}