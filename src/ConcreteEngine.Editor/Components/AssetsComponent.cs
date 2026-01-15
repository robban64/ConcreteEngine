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


    public AssetsComponent()
    {
        _assetList = new DrawAssetList(this);
        _drawMaterialProperty = new DrawMaterialProperty(this);
        _assetFiles = new DrawAssetFiles(this);
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

        
        _assetList.DrawList(state, in ctx);

        ImGui.EndTable();
    }

    public override void DrawRight(AssetState state, in FrameContext ctx)
    {
        var proxy = state.Proxy;
        if (proxy is null) return;
        if (!ImGui.BeginChild("##asset-sidebar-properties"u8, ImGuiChildFlags.None)) return;

        DrawSelectedInfo(state, proxy, in ctx);
        ImGui.Separator();
        if (proxy.Property is MaterialProxyProperty matProp)
            _drawMaterialProperty.DrawMaterialProperties(matProp, in ctx);

        ImGui.EndChild();
    }
    
    public void DrawSelectedInfo(AssetState state, AssetProxy proxy, in FrameContext ctx)
    {
        var za = ctx.GetWriter();
        var asset = proxy.Asset;
        var fileSpecs = proxy.FileSpecs;

        RefGui.SeparatorTextId(asset.Kind.ToTextUtf8(), asset.Id, ref za);
        
        RefGui.DrawRightProp("Name:"u8, ref za.Append(asset.Name));
        
        _assetFiles.Draw(state, ref za);
        ImGui.SameLine();
        RefGui.TextUnformatted(ref za.Append("Files: "u8).AppendEnd(fileSpecs.Length));

        RefGui.DrawRightProp("GID:"u8, ref za.Append(proxy.GIdString));
        RefGui.DrawRightProp("Generation:"u8, ref za.Append(asset.Generation));

    }
    
    

}