using System.Numerics;
using ConcreteEngine.Core.Common.Memory;
using ConcreteEngine.Core.Common.Text;
using ConcreteEngine.Core.Engine.Assets;
using ConcreteEngine.Core.Engine.Assets.Data;
using ConcreteEngine.Core.Renderer.Material;
using ConcreteEngine.Editor.Controller;
using ConcreteEngine.Editor.Controller.Proxy;
using ConcreteEngine.Editor.Core;
using ConcreteEngine.Editor.Lib;
using ConcreteEngine.Editor.UI;
using ConcreteEngine.Editor.UI.Widgets;
using ConcreteEngine.Editor.Utils;
using ConcreteEngine.Graphics.Gfx.Contracts;
using ConcreteEngine.Graphics.Gfx.Definitions;
using Hexa.NET.ImGui;

namespace ConcreteEngine.Editor.Panels.Assets;

internal sealed class MaterialPropertyUi
{
    private readonly PanelContext _panelContext;
    private readonly AssetController _assetController;

    public MaterialPropertyUi(PanelContext panelContext, AssetController assetController)
    {
        _panelContext = panelContext;
        _assetController = assetController;
    }

    public void DrawMaterialProperties(EditorMaterial material, in FrameContext ctx)
    {
        ImGui.BeginGroup();
        if (material.Asset.TemplateId.IsValid())
        {
            var template = _assetController.GetAsset<Material>(material.Asset.TemplateId);
            ImGui.TextUnformatted("Template: "u8);
            ImGui.SameLine();
            ImGui.TextColored(StyleMap.GetAssetColor(AssetKind.Material), ref ctx.Sw.Write(template.Name));
        }

        if (material.Asset.AssetShader.IsValid())
        {
            var shader = _assetController.GetAsset<Shader>(material.Asset.AssetShader);
            ImGui.TextUnformatted("Shader: "u8);
            ImGui.SameLine();
            ImGui.TextColored(StyleMap.GetAssetColor(AssetKind.Shader), ref ctx.Sw.Write(shader.Name));
        }

        ImGui.EndGroup();

        ImGui.Spacing();
        ImGui.SeparatorText("Texture Slots"u8);

        DrawTextureSlots(material.Asset, ctx.Sw);

        ImGui.SeparatorText("Base Parameters"u8);
        material.ColorField.DrawField(true);
        material.SpecularField.DrawField(false);
        material.ShininessField.DrawField(false);
        material.UvRepeatField.DrawField(false);

        DrawPipeline(material, ctx.Sw);

        ImGui.Separator();

        DrawPassFunctions(material);
    }

    private static void DrawPipeline(EditorMaterial material, UnsafeSpanWriter sw)
    {
        var passState = material.Asset.Pipeline.PassState;

        ImGui.SeparatorText("Pipeline State"u8);
        DrawFlagToggle("Blend Mode"u8, GfxStateFlags.Blend, ref passState, sw);
        DrawFlagToggle("Cull Mode"u8, GfxStateFlags.Cull, ref passState, sw);
        DrawFlagToggle("Depth Test"u8, GfxStateFlags.DepthTest, ref passState, sw);
        DrawFlagToggle("Depth Write"u8, GfxStateFlags.DepthWrite, ref passState, sw);
        DrawFlagToggle("Polygon Offset"u8, GfxStateFlags.PolygonOffset, ref passState, sw);

        if (material.Asset.Pipeline.PassState != passState)
            material.Asset.SetPassState(passState);

        return;

        static void DrawFlagToggle(ReadOnlySpan<byte> label, GfxStateFlags flag, ref GfxPassState state,
            UnsafeSpanWriter sw)
        {
            var isDefined = state.IsSet(flag);
            if (ImGui.Checkbox(ref sw.Start(label).Append("##1-"u8).Append((int)flag).End(), ref isDefined))
                state = new GfxPassState(state.Enabled, state.Defined ^ flag);

            if (!isDefined) return;

            ImGui.SameLine(130);

            var isEnabled = state.IsSet(flag);
            if (ImGui.Checkbox(ref sw.Start("##2-"u8).Append((int)flag).End(), ref isEnabled))
                state = new GfxPassState(state.Enabled ^ flag, state.Defined);
        }
    }

    private static void DrawPassFunctions(EditorMaterial material)
    {
        var passState = material.PassState;
        if (passState.IsEmpty) return;
        ImGui.PushItemWidth(110);

        if (passState.IsSet(GfxStateFlags.Blend))
            material.BlendCombo.DrawField(false);

        if (passState.IsSet(GfxStateFlags.Cull))
            material.CullCombo.DrawField(false);

        if (passState.IsSet(GfxStateFlags.DepthTest))
            material.DepthCombo.DrawField(false);

        if (passState.IsSet(GfxStateFlags.PolygonOffset))
            material.PolygonCombo.DrawField(false);

        ImGui.PopItemWidth();
    }


    private void DrawTextureSlots(Material asset, UnsafeSpanWriter sw)
    {
        const ImGuiTableFlags flags = ImGuiTableFlags.SizingStretchProp | ImGuiTableFlags.RowBg |
                                      ImGuiTableFlags.BordersInnerH;

        if (!ImGui.BeginTable("##mat_tex_table"u8, 2, flags)) return;

        ImGui.TableSetupColumn("Label"u8, ImGuiTableColumnFlags.None, 0.35f);
        ImGui.TableSetupColumn("Slot"u8, ImGuiTableColumnFlags.WidthStretch);

        var usageNames = EnumCache<TextureUsage>.Names;
        var bindings = asset.GetTextureSources();

        for (var i = 0; i < bindings.Length; i++)
        {
            var binding = bindings[i];
            ImGui.PushID(i);
            ImGui.TableNextRow();

            ImGui.TableNextColumn();
            ImGui.TextUnformatted(ref sw.Write(usageNames[(int)binding.Usage]));

            DrawHover(binding, sw);

            ImGui.TableNextColumn();
            if (binding.Texture.IsValid())
                DrawAssetSlot(asset, i, _assetController.GetAsset<Texture>(binding.Texture));
            else
                DrawAssetSlotEmptyTexture(asset, i, binding.IsFallback);

            ImGui.PopID();
        }

        ImGui.EndTable();
        return;

        static void DrawHover(TextureSource binding, UnsafeSpanWriter sw)
        {
            if (!ImGui.IsItemHovered()) return;

            ImGui.BeginTooltip();
            ImGui.TextUnformatted("Binding Info"u8);
            ImGui.Separator();

            ImGui.TextUnformatted(ref sw.Start("Kind: "u8)
                .Append(binding.TextureKind.ToText())
                .Append("\nFormat: "u8)
                .Append(binding.PixelFormat.ToText())
                .End());

            ImGui.EndTooltip();
        }
    }


    private unsafe void DrawAssetSlot(Material material, int slot, Texture slotTexture)
    {
        var rowHeight = ImGui.GetFrameHeight();

        var clearBtnWidth = rowHeight + ImGui.GetStyle().ItemSpacing.X;
        var contentWidth = ImGui.GetContentRegionAvail().X - clearBtnWidth;

        if (ImGui.Button(slotTexture.Name, new Vector2(contentWidth, rowHeight)))
            ImGui.OpenPopup("preview-popup"u8);
        
        DropTexture(material, slot);

        ImGui.SameLine();
        if (slotTexture.Id.IsValid() && ImGui.Button("X"u8, new Vector2(rowHeight, rowHeight)))
            material.SetTexture(slot, null);

        if (ImGui.IsItemHovered())
        {
            ImGui.BeginTooltip();
            ImGui.TextUnformatted("Clear Slot"u8);
            ImGui.EndTooltip();
        }

        if (ImGui.BeginPopup("##preview-popup"u8))
        {
            var texPtr = _panelContext.GetTextureRefPtr(slotTexture.GfxId);
            ImGui.Image(*texPtr.Handle, new Vector2(256, 256));

            if (ImGui.Button("Close"u8)) ImGui.CloseCurrentPopup();
            ImGui.EndPopup();
        }
    }

    private  unsafe void DrawAssetSlotEmptyTexture(Material material, int slot, bool isFallback)
    {
        var rowHeight = ImGui.GetFrameHeight();
        var contentWidth = ImGui.GetContentRegionAvail().X;

        ImGui.PushStyleColor(ImGuiCol.Text, isFallback ? Palette.RedBase : Palette.GrayBase);

        var text = isFallback ? "Missing Asset"u8 : "-"u8;
        if (ImGui.Button(text, new Vector2(contentWidth, rowHeight)))
        {
        }

        DropTexture(material, slot);

        ImGui.PopStyleColor();
    }

    private unsafe void DropTexture(Material material, int slot)
    {
        if (!ImGui.BeginDragDropTarget()) return;

        var payload = ImGui.AcceptDragDropPayload("ASSET_TEXTURE"u8);
        if (!payload.IsNull && payload.IsDelivery())
        {
            var droppedId = *(AssetId*)payload.Data;
            if (droppedId > 0 && _assetController.TryGetAsset<Texture>(droppedId, out var droppedTex))
                material.SetTexture(slot, droppedTex);
        }

        ImGui.EndDragDropTarget();

    }

    /*
ImGui.PushStyleColor(ImGuiCol.Button, new Vector4(0.2f, 0.2f, 0.2f, 1.0f));
ImGui.Button(""u8, new Vector2(buttonHeight, buttonHeight));
ImGui.PopStyleColor();

if (ImGui.BeginDragDropTarget())
{
    // var payload = ImGui.AcceptDragDropPayload("ASSET_TEXTURE"u8);
    ImGui.EndDragDropTarget();
}

ImGui.SameLine();
*/
}