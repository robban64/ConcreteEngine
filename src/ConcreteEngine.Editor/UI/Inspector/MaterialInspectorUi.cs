using System.Numerics;
using System.Runtime.CompilerServices;
using ConcreteEngine.Core.Common.Memory;
using ConcreteEngine.Core.Common.Text;
using ConcreteEngine.Core.Engine.Assets;
using ConcreteEngine.Core.Engine.Assets.Data;
using ConcreteEngine.Core.Renderer.Material;
using ConcreteEngine.Editor.Bridge;
using ConcreteEngine.Editor.Core;
using ConcreteEngine.Editor.Theme;
using ConcreteEngine.Editor.Utils;
using ConcreteEngine.Graphics.Gfx.Contracts;
using Hexa.NET.ImGui;

namespace ConcreteEngine.Editor.UI.Inspector;

internal sealed unsafe class MaterialInspectorUi(StateContext panelContext)
{
    public void Draw(InspectMaterial material, FrameContext ctx)
    {
        ImGui.SeparatorText("Material Info"u8);
        ImGui.BeginGroup();
        if (material.Asset.TemplateId.IsValid())
        {
            var template = EngineObjectStore.AssetController.GetAsset<Material>(material.Asset.TemplateId);
            ImGui.TextUnformatted("Template: "u8);
            ImGui.SameLine();
            ImGui.TextColored(StyleMap.GetAssetColor(AssetKind.Material), ctx.Sw.Write(template.Name));
        }

        if (material.Asset.AssetShader.IsValid())
        {
            var shader = EngineObjectStore.AssetController.GetAsset<Shader>(material.Asset.AssetShader);
            ImGui.TextUnformatted("Shader: "u8);
            ImGui.SameLine();
            ImGui.TextColored(StyleMap.GetAssetColor(AssetKind.Shader), ctx.Sw.Write(shader.Name));
        }

        ImGui.EndGroup();

        ImGui.Spacing();
        ImGui.SeparatorText("Texture Slots"u8);
        DrawTextureSlots(material.Asset, ctx);

        ImGui.SeparatorText("State Properties"u8);
        material.ColorField.Draw();
        material.SpecularField.Draw();
        material.ShininessField.Draw();
        material.UvRepeatField.Draw();

        ImGui.Spacing();
        DrawPipeline(material, ctx);
    }

    private static void DrawPipeline(InspectMaterial editMaterial, FrameContext ctx)
    {
        var passState = editMaterial.PassState;

        ImGui.SeparatorText("State Flag"u8);
        DrawFlagToggle("Blend Mode"u8, GfxStateFlags.Blend, ref passState, ctx.Sw);
        DrawFlagToggle("Cull Mode"u8, GfxStateFlags.Cull, ref passState, ctx.Sw);
        DrawFlagToggle("Depth Test"u8, GfxStateFlags.DepthTest, ref passState, ctx.Sw);
        DrawFlagToggle("Depth Write"u8, GfxStateFlags.DepthWrite, ref passState, ctx.Sw);
        DrawFlagToggle("Polygon Offset"u8, GfxStateFlags.PolygonOffset, ref passState, ctx.Sw);

        if (editMaterial.PassState != passState)
            editMaterial.Asset.SetPassState(passState);

        if (passState.IsEmpty) return;

        ImGui.Spacing();
        ImGui.SeparatorText("State Value"u8);

        ImGui.PushItemWidth(110);

        if (passState.IsSet(GfxStateFlags.Blend))
            editMaterial.BlendCombo.Draw();

        if (passState.IsSet(GfxStateFlags.Cull))
            editMaterial.CullCombo.Draw();

        if (passState.IsSet(GfxStateFlags.DepthTest))
            editMaterial.DepthCombo.Draw();

        if (passState.IsSet(GfxStateFlags.PolygonOffset))
            editMaterial.PolygonCombo.Draw();

        ImGui.PopItemWidth();
    }

    private void DrawTextureSlots(Material asset, FrameContext ctx)
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
            ImGui.TextUnformatted(ctx.Sw.Write(usageNames[(int)binding.Usage]));

            DrawHover(binding, ctx.Sw);

            ImGui.TableNextColumn();
            if (binding.Texture.IsValid())
                DrawAssetSlot(asset, i, EngineObjectStore.AssetController.GetAsset<Texture>(binding.Texture), ctx);
            else
                DrawAssetSlotEmptyTexture(asset, i, binding, ctx);

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

            ImGui.TextUnformatted(ref sw.Append("Kind: "u8)
                .Append(binding.TextureKind.ToText())
                .Append("\nFormat: "u8)
                .Append(binding.PixelFormat.ToText())
                .End());

            ImGui.EndTooltip();
        }
    }


    private void DrawAssetSlot(Material material, int slot, Texture slotTexture, FrameContext ctx)
    {
        var rowHeight = ImGui.GetFrameHeight();
        var clearBtnWidth = rowHeight + ImGui.GetStyle().ItemSpacing.X;
        var contentWidth = ImGui.GetContentRegionAvail().X - clearBtnWidth;

        if (ImGui.Button(ctx.Sw.Write(slotTexture.Name), new Vector2(contentWidth, rowHeight)))
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
            var texPtr = panelContext.GetTextureRefPtr(slotTexture.GfxId);
            ImGui.Image(*texPtr.Handle, new Vector2(256, 256));

            if (ImGui.Button("Close"u8)) ImGui.CloseCurrentPopup();
            ImGui.EndPopup();
        }
    }

    private void DrawAssetSlotEmptyTexture(Material material, int slot, TextureSource source, FrameContext ctx)
    {
        var size = new Vector2(ImGui.GetContentRegionAvail().X, ImGui.GetFrameHeight());

        if (source.IsFallback)
        {
            ImGui.PushStyleColor(ImGuiCol.Text, Palette.GrayBase);
            ImGui.Button(ctx.Sw.Write(source.GetFallbackName()), size);
            ImGui.PopStyleColor();
            return;
        }

        ImGui.PushStyleColor(ImGuiCol.Text, Palette.OrangeBase);
        ImGui.Button("Empty Slot"u8, size);
        DropTexture(material, slot);

        ImGui.PopStyleColor();
    }

    private void DropTexture(Material material, int slot)
    {
        if (!ImGui.BeginDragDropTarget()) return;

        var payload = ImGui.AcceptDragDropPayload("ASSET_TEXTURE"u8);
        if (!payload.IsNull && payload.IsDelivery())
        {
            var droppedId = *(AssetId*)payload.Data;
            if (droppedId > 0 && EngineObjectStore.AssetController.TryGetAsset<Texture>(droppedId, out var droppedTex))
                material.SetTexture(slot, droppedTex);
        }

        ImGui.EndDragDropTarget();
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static void DrawFlagToggle(ReadOnlySpan<byte> label, GfxStateFlags flag, ref GfxPassState state,
        UnsafeSpanWriter sw)
    {
        var isDefined = state.IsSet(flag);
        if (ImGui.Checkbox(ref sw.Append(label).Append("##1-"u8).Append((int)flag).End(), ref isDefined))
            state = new GfxPassState(state.Enabled, state.Defined ^ flag);

        if (!isDefined) return;

        ImGui.SameLine(130);

        var isEnabled = state.IsEnabled(flag);
        if (ImGui.Checkbox(ref sw.Append("##2-"u8).Append((int)flag).End(), ref isEnabled))
            state = new GfxPassState(state.Enabled ^ flag, state.Defined);
    }
}