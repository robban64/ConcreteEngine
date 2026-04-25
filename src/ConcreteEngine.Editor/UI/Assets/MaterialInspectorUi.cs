using System.Numerics;
using System.Runtime.CompilerServices;
using ConcreteEngine.Core.Common.Memory;
using ConcreteEngine.Core.Common.Numerics;
using ConcreteEngine.Core.Common.Text;
using ConcreteEngine.Core.Engine.Assets;
using ConcreteEngine.Core.Engine.Assets.Data;
using ConcreteEngine.Core.Renderer.Material;
using ConcreteEngine.Editor.Core;
using ConcreteEngine.Editor.Data;
using ConcreteEngine.Editor.Inspector;
using ConcreteEngine.Editor.Inspector.Impl;
using ConcreteEngine.Editor.Lib;
using ConcreteEngine.Editor.Theme;
using ConcreteEngine.Editor.Utils;
using ConcreteEngine.Graphics.Gfx.Contracts;
using Hexa.NET.ImGui;

namespace ConcreteEngine.Editor.UI.Assets;

internal sealed unsafe class MaterialInspectorUi(StateManager state)
{
    private static AssetProvider AssetProvider => EngineObjectStore.AssetProvider;

    public readonly InspectMaterialFields InspectFields = InspectorFieldProvider.Instance.MaterialFields;

    public void Draw(InspectMaterial material, FrameContext ctx)
    {
        ImGui.SeparatorText("Material Info"u8);
        ImGui.BeginGroup();
        if (material.Asset.TemplateId.IsValid())
        {
            var template = AssetProvider.Get<Material>(material.Asset.TemplateId);
            ImGui.TextUnformatted("Template: "u8);
            ImGui.SameLine();
            //ImGui.TextColored(StyleMap.GetAssetColor(AssetKind.Material), ctx.Sw.Write(template.Name));
            ImGui.TextColored(Color4.White, ctx.Sw.Write(template.Name));
        }

        if (material.Asset.ShaderId.IsValid())
        {
            var shader = AssetProvider.Get<Shader>(material.Asset.ShaderId);
            ImGui.TextUnformatted("Shader: "u8);
            ImGui.SameLine();
            ImGui.TextColored(Color4.White, ctx.Sw.Write(shader.Name));
            //ImGui.TextColored(StyleMap.GetAssetColor(AssetKind.Shader), ctx.Sw.Write(shader.Name));
        }

        ImGui.EndGroup();

        ImGui.Spacing();
        ImGui.SeparatorText("Texture Slots"u8);
        DrawTextureSlots(material.Asset, ctx);

        ImGui.SeparatorText("State Properties"u8);
        InspectFields.Draw(0, 1);

        ImGui.Spacing();
        DrawPipeline(material, ctx);
    }

    private void DrawPipeline(InspectMaterial editMaterial, FrameContext ctx)
    {
        var passState = editMaterial.PassState;
        var sw = ctx.Sw;
        ImGui.SeparatorText("State Flag"u8);
        DrawFlagToggle("Blend Mode"u8, GfxStateFlags.Blend, ref passState, sw);
        DrawFlagToggle("Cull Mode"u8, GfxStateFlags.Cull, ref passState, sw);
        DrawFlagToggle("Depth Test"u8, GfxStateFlags.DepthTest, ref passState, sw);
        DrawFlagToggle("Depth Write"u8, GfxStateFlags.DepthWrite, ref passState, sw);
        DrawFlagToggle("Polygon Offset"u8, GfxStateFlags.PolygonOffset, ref passState, sw);

        if (editMaterial.PassState != passState)
            editMaterial.Asset.SetPassState(passState);

        if (passState.IsEmpty) return;

        ImGui.Spacing();
        ImGui.SeparatorText("State Value"u8);

        ImGui.PushItemWidth(110);

        if (passState.IsSet(GfxStateFlags.Blend))
            InspectFields.BlendCombo.Draw();

        if (passState.IsSet(GfxStateFlags.Cull))
            InspectFields.CullCombo.Draw();

        if (passState.IsSet(GfxStateFlags.DepthTest))
            InspectFields.DepthCombo.Draw();

        if (passState.IsSet(GfxStateFlags.PolygonOffset))
            InspectFields.PolygonCombo.Draw();

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
            AppDraw.Text(ctx.Sw.Append(usageNames[(int)binding.Usage]).End());

            DrawHover(binding, ctx.Sw);

            ImGui.TableNextColumn();
            if (binding.Texture.IsValid())
                DrawAssetSlot(asset, i, AssetProvider.Get<Texture>(binding.Texture), ctx);
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

            AppDraw.Text(sw.Append("Kind: "u8)
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
            if (!state.TryGetTextureRefPtr(slotTexture.GfxId, out var texPtr))
                ImGui.TextUnformatted("Invalid Texture"u8);
            else
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
            if (droppedId > 0 && AssetProvider.TryGet<Texture>(droppedId, out var droppedTex))
                material.SetTexture(slot, droppedTex);
        }

        ImGui.EndDragDropTarget();
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static void DrawFlagToggle(ReadOnlySpan<byte> label, GfxStateFlags flag, ref GfxPassState state,
        UnsafeSpanWriter sw)
    {
        var isDefined = state.IsSet(flag);
        if (ImGui.Checkbox(sw.Append(label).Append("##1-"u8).Append((int)flag).End(), ref isDefined))
            state = new GfxPassState(state.Enabled, state.Defined ^ flag);

        if (!isDefined) return;

        ImGui.SameLine(130);

        var isEnabled = state.IsEnabled(flag);
        if (ImGui.Checkbox(sw.Append("##2-"u8).Append((int)flag).End(), ref isEnabled))
            state = new GfxPassState(state.Enabled ^ flag, state.Defined);
    }
}