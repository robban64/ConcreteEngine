using System.Numerics;
using ConcreteEngine.Core.Engine.Assets;
using ConcreteEngine.Editor.Bridge;
using ConcreteEngine.Editor.Components.Assets;
using ConcreteEngine.Editor.Core;
using ConcreteEngine.Editor.Data;
using ConcreteEngine.Editor.Definitions;
using ConcreteEngine.Editor.UI;
using ConcreteEngine.Editor.Utils;
using Hexa.NET.ImGui;

namespace ConcreteEngine.Editor.Components;

internal sealed class AssetsComponent : EditorComponent<AssetState>
{
    private readonly DrawAssetList _assetList;
    private readonly DrawAssetFiles _assetFiles;

    private readonly DrawTextureProperty _textureProxy;
    private readonly DrawMaterialProperty _materialProxy;

    private readonly ClipDrawer _clipDrawer;

    public AssetsComponent()
    {
        _assetList = new DrawAssetList(this);
        _materialProxy = new DrawMaterialProperty(this);
        _assetFiles = new DrawAssetFiles(this);
        _textureProxy = new DrawTextureProperty(this);

        _clipDrawer = new ClipDrawer(_assetList.DrawListItem);
    }

    public void TriggerSelection(AssetId id) => TriggerEvent(EventKey.SelectionChanged, id);

    public void TriggerTextureUpdate(TextureProxyProperty prop, string name, int value) =>
        TriggerEvent(EventKey.SelectionUpdated, value);

    public override void DrawLeft(AssetState state, ref FrameContext ctx)
    {
        ImGui.SeparatorText("Asset Store"u8);

        _assetList.DrawTypeSelector(state, ref ctx);

        if (state.ShowKind == AssetKind.Unknown) return;
        if (!ImGui.BeginTable("##asset_store_object_tbl"u8, 3, GuiTheme.TableFlags)) return;

        ImGui.TableSetupColumn("Type"u8, DrawAssetList.ColumnWidth);
        ImGui.TableSetupColumn("Id"u8, DrawAssetList.ColumnWidth);
        ImGui.TableSetupColumn("Name"u8, ImGuiTableColumnFlags.WidthStretch);
        ImGui.TableHeadersRow();

        var len = state.GeAssetSpan().Length;
        _clipDrawer.Draw(len, DrawAssetList.PaddedRowHeight, ref ctx.Sw);

        ImGui.EndTable();
    }

    public override void DrawRight(AssetState state, ref FrameContext ctx)
    {
        var proxy = state.Proxy;
        if (proxy is null) return;
        if (!ImGui.BeginChild("##asset-sidebar-props"u8, ImGuiChildFlags.AlwaysUseWindowPadding))
            return;

        DrawSelectedInfo(state, proxy, ref ctx);
        ImGui.Separator();

        switch (proxy.Property)
        {
            case ShaderProxyProperty shaderProp:
                DrawShaderProperties(proxy, shaderProp);
                break;
            case ModelProxyProperty modelProxy:
                DrawModelProperties(modelProxy, ref ctx);
                break;
            case TextureProxyProperty texProp:
                _textureProxy.Draw(texProp, ref ctx);
                break;

            case MaterialProxyProperty matProp:
                _materialProxy.DrawMaterialProperties(matProp, ref ctx);
                break;
        }
        ImGui.EndChild();
    }

    public void DrawSelectedInfo(AssetState state, AssetProxy proxy, ref FrameContext ctx)
    {
        var asset = proxy.Asset;
        var fileSpecs = proxy.FileSpecs;
        ref var sw = ref ctx.Sw;

        var layout = new TextLayout()
            .TitleWithId(ref sw, asset.Kind.ToTextUtf8(), asset.Id)
            .Property("Name:"u8, sw.Write(asset.Name));

        _assetFiles.Draw(state, ref sw);
        ImGui.SameLine();

        layout.Property("Files:"u8, sw.Write(fileSpecs.Length))
            .Property("GID:"u8, sw.Write(proxy.GIdString))
            .Property("Generation:"u8, sw.Write(asset.Generation));
    }

    public void DrawShaderProperties(AssetProxy proxy, ShaderProxyProperty prop)
    {
        var layout = new TextLayout();
        ImGui.Spacing();

        // The Action Area
        if (ImGui.Button("Reload Shader"u8, new Vector2(-1, 0)))
            TriggerEvent(EventKey.SelectionAction, proxy.Asset.Name);

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

    public void DrawModelProperties(ModelProxyProperty prop, ref FrameContext ctx)
    {
        var asset = prop.Asset;

        ref var sw = ref ctx.Sw;
        var layout = new TextLayout();

        ImGui.SeparatorText("Model Statistics"u8);
        layout.Property("Total Tris:"u8, sw.Write(asset.DrawCount))
            .Property("Mesh Count:"u8, sw.Write(asset.MeshCount))
            .Property("Animated:"u8, StrUtils.BoolToYesNoShort(asset.IsAnimated));

        ImGui.Spacing();

        ImGui.SeparatorText("Mesh Parts"u8);

        var meshes = prop.Meshes;
        for (int i = 0; i < meshes.Length; i++)
        {
            var mesh = prop.Meshes[i];
            if (ImGui.TreeNodeEx(sw.Start(i).Append(mesh.Name).End(), ImGuiTreeNodeFlags.SpanFullWidth))
            {
                layout.Property("Index:"u8, sw.Write(mesh.Spec.MeshIndex))
                    .Property("Material ID:"u8, sw.Write(mesh.Spec.MaterialIndex))
                    .Property("Tris:"u8, sw.Write(mesh.Spec.DrawCount));

                ImGui.TreePop();
            }
        }

        if (asset.IsAnimated)
        {
            layout.Property("Bone Count:"u8, sw.Write(prop.BoneCount));

            ImGui.Spacing();
            ImGui.SeparatorText("Animation Clips"u8);

            if (ImGui.BeginTable("##anim_table"u8, 4,  GuiTheme.TableFlags))
            {
                ImGui.TableSetupColumn("Name"u8);
                ImGui.TableSetupColumn("Duration"u8);
                ImGui.TableSetupColumn("TPS"u8);
                ImGui.TableSetupColumn("Track"u8, 28);

                ImGui.TableHeadersRow();

                foreach (var clip in prop.Clips)
                {
                    ImGui.TableNextRow();
//layout.NextColumn(sw.Write(clip.Name)).NextColumn(clip.Duration).NextColumn(clip.TicksPerSecond);
                    ImGui.TableSetColumnIndex(0);
                    ImGui.TextUnformatted(sw.Write(clip.Name));

                    ImGui.TableSetColumnIndex(1);
                    ImGui.TextUnformatted(sw.Write(clip.Duration));

                    ImGui.TableSetColumnIndex(2);
                    ImGui.TextUnformatted(sw.Write(clip.TicksPerSecond));

                    ImGui.TableSetColumnIndex(3);
                    ImGui.TextUnformatted(sw.Write(clip.TrackCount));
                }

                ImGui.EndTable();
            }
        }
    }
}
