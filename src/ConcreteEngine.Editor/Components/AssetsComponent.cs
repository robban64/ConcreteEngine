using System.Numerics;
using System.Runtime.CompilerServices;
using ConcreteEngine.Core.Common.Memory;
using ConcreteEngine.Core.Engine.Assets;
using ConcreteEngine.Editor.Bridge;
using ConcreteEngine.Editor.Components.Assets;
using ConcreteEngine.Editor.Components.Draw;
using ConcreteEngine.Editor.Core;
using ConcreteEngine.Editor.Data;
using ConcreteEngine.Editor.Definitions;
using ConcreteEngine.Editor.UI;
using ConcreteEngine.Editor.Utils;
using Hexa.NET.ImGui;
using ZaString.Core;
using ZaString.Extensions;
using static ConcreteEngine.Editor.UI.GuiUtils;

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

    public override void DrawLeft(AssetState state, in FrameContext ctx)
    {
        ImGui.SeparatorText("Asset Store"u8);

        _assetList.DrawTypeSelector(state);

        if (state.ShowKind == AssetKind.Unknown) return;
        if (!ImGui.BeginTable("##asset_store_object_tbl"u8, 3, GuiTheme.TableFlags)) return;

        ImGui.TableSetupColumn("Type"u8, DrawAssetList.ColumnWidth);
        ImGui.TableSetupColumn("Id"u8, DrawAssetList.ColumnWidth);
        ImGui.TableSetupColumn("Name"u8, ImGuiTableColumnFlags.WidthStretch);
        ImGui.TableHeadersRow();

        var len = state.GeAssetSpan().Length;
        var writer = ctx.Writer;
        _clipDrawer.Draw(len, DrawAssetList.PaddedRowHeight, ref writer);

        ImGui.EndTable();
    }

    public override void DrawRight(AssetState state, in FrameContext ctx)
    {
        var proxy = state.Proxy;
        if (proxy is null) return;
        if (!ImGui.BeginChild("##asset-sidebar-properties"u8, ImGuiChildFlags.None)) return;

        var sw = ctx.Writer;
        DrawSelectedInfo(state, proxy, ref sw);
        ImGui.Separator();
        if (proxy.Property is MaterialProxyProperty matProp)
            _drawMaterialProperty.DrawMaterialProperties(matProp, ref sw);

        ImGui.EndChild();
    }

    public void DrawSelectedInfo(AssetState state, AssetProxy proxy, ref SpanWriter sw)
    {
        var asset = proxy.Asset;
        var fileSpecs = proxy.FileSpecs;

        DrawContext.SeparatorTextId(ref sw, asset.Kind.ToTextUtf8(), asset.Id);

        DrawContext.DrawRightProp(sw.Write(asset.Name), "Name:"u8);

        _assetFiles.Draw(state, ref sw);
        ImGui.SameLine();
        ImGui.TextUnformatted(sw.Start("Files: "u8).Append(fileSpecs.Length).End());
        DrawContext.DrawRightProp(sw.Write(proxy.GIdString), "GID:"u8);
        DrawContext.DrawRightProp(sw.Write(asset.Generation), "Generation:"u8);
    }
}