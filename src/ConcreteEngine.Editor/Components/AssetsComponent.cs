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
    private readonly DrawMaterialProperty _drawMaterialProperty;
    private readonly DrawAssetFiles _assetFiles;
    
    private readonly ClipDrawer _clipDrawer;

    public AssetsComponent()
    {
        _assetList = new DrawAssetList(this);
        _drawMaterialProperty = new DrawMaterialProperty(this);
        _assetFiles = new DrawAssetFiles(this);
        
        _clipDrawer = new ClipDrawer(_assetList.DrawListItem);
    }

    public void TriggerSelection(AssetId id) => TriggerEvent(EventKey.SelectionChanged, id);

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
        if (!ImGui.BeginChild("##asset-sidebar-properties"u8, ImGuiChildFlags.None)) return;

        DrawSelectedInfo(state, proxy, ref ctx);
        ImGui.Separator();
        if (proxy.Property is MaterialProxyProperty matProp)
            _drawMaterialProperty.DrawMaterialProperties(matProp, ref ctx);

        ImGui.EndChild();
    }

    public void DrawSelectedInfo(AssetState state, AssetProxy proxy, ref FrameContext ctx)
    {
        var asset = proxy.Asset;
        var fileSpecs = proxy.FileSpecs;
        ref var sw = ref ctx.Sw;
        DrawGui.SeparatorTextId(ref sw, asset.Kind.ToTextUtf8(), asset.Id);

        var layout = new TextLayout().DrawProperty("Name:"u8, sw.Write(asset.Name));
        _assetFiles.Draw(state, ref sw);
        ImGui.SameLine();
        layout.DrawProperty("Files:"u8, sw.Write(fileSpecs.Length))
            .DrawProperty("GID:"u8, sw.Write(proxy.GIdString))
            .DrawProperty("Generation:"u8, sw.Write(asset.Generation));

    }
}