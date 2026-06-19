using System.Numerics;
using System.Runtime.CompilerServices;
using ConcreteEngine.Core.Common;
using ConcreteEngine.Core.Common.Numerics;
using ConcreteEngine.Core.Common.Text;
using ConcreteEngine.Core.Engine.Assets;
using ConcreteEngine.Editor.Core;
using ConcreteEngine.Editor.Data;
using ConcreteEngine.Editor.Inspector;
using ConcreteEngine.Editor.Inspector.Impl;
using ConcreteEngine.Editor.Theme;
using ConcreteEngine.Graphics.Gfx;
using ConcreteEngine.Renderer.Core;
using Hexa.NET.ImGui;

namespace ConcreteEngine.Editor.UI.Assets;

internal sealed unsafe class MaterialInspectorUi(StateManager state)
{

    public readonly InspectMaterialFields InspectFields = InspectorFieldProvider.Instance.MaterialFields;

    public void Draw(InspectMaterial material)
    {
        var sw = TextBuffers.GetWriter();

        ImGui.SeparatorText("Material Info"u8);
        ImGui.BeginGroup();
        ImGui.TextUnformatted("Shader: "u8);
        ImGui.SameLine();
        ImGui.TextColored(Color4.White, sw.Write(material.Asset.BoundShader.Name));
        ImGui.EndGroup();

        ImGui.Spacing();
        ImGui.SeparatorText("Texture Slots"u8);
        DrawTextureSlots(material.Asset, sw);

        ImGui.SeparatorText("State Properties"u8);
        InspectFields.Draw(0, 1);

        ImGui.Spacing();
        DrawPipeline(material, sw);
    }

    private void DrawPipeline(InspectMaterial editMaterial, NativeSpanWriter sw)
    {
        var ogDrawState = editMaterial.State.DrawState;
        var drawState = editMaterial.State.DrawState;
        ImGui.SeparatorText("State Flag"u8);
        DrawFlagToggle("Blend Mode"u8, GfxDrawFlags.Blend, ref drawState, sw);
        DrawFlagToggle("Cull Mode"u8, GfxDrawFlags.Cull, ref drawState, sw);
        DrawFlagToggle("Depth Test"u8, GfxDrawFlags.DepthTest, ref drawState, sw);
        DrawFlagToggle("Depth Write"u8, GfxDrawFlags.DepthWrite, ref drawState, sw);
        DrawFlagToggle("Polygon Offset"u8, GfxDrawFlags.PolygonOffset, ref drawState, sw);
        ImGui.Separator();
        DrawFlagToggle("A2C"u8, GfxDrawFlags.Ac2, ref drawState, sw);

        if (ogDrawState != drawState)
            editMaterial.State.DrawState = drawState;

        if (drawState.IsEmpty()) return;

        ImGui.Spacing();
        ImGui.SeparatorText("State Value"u8);

        ImGui.PushItemWidth(110);

        if (drawState.IsSet(GfxDrawFlags.Blend))
            InspectFields.BlendCombo.Draw();

        if (drawState.IsSet(GfxDrawFlags.Cull))
            InspectFields.CullCombo.Draw();

        if (drawState.IsSet(GfxDrawFlags.DepthTest))
            InspectFields.DepthCombo.Draw();

        if (drawState.IsSet(GfxDrawFlags.PolygonOffset))
            InspectFields.PolygonCombo.Draw();

        ImGui.PopItemWidth();
    }

    private void DrawTextureSlots(Material asset, NativeSpanWriter sw)
    {
        const ImGuiTableFlags flags = ImGuiTableFlags.SizingStretchProp | ImGuiTableFlags.RowBg |
                                      ImGuiTableFlags.BordersInnerH;

        if (!ImGui.BeginTable("##mat_tex_table"u8, 2, flags)) return;

        ImGui.TableSetupColumn("Label"u8, ImGuiTableColumnFlags.None, 0.35f);
        ImGui.TableSetupColumn("Slot"u8, ImGuiTableColumnFlags.WidthStretch);

        var usageNames = EnumCache<TextureUsage>.Names;
        var bindings = asset.GetSourceSpan();

        for (var i = 0; i < bindings.Length; i++)
        {
            var binding = bindings[i];
            ImGui.PushID(i);
            ImGui.TableNextRow();

            ImGui.TableNextColumn();
            AppDraw.Text(sw.Append(usageNames[(int)binding.Usage]).End());

            ImGui.TableNextColumn();
            if (binding.AssetTexture.IsValid())
                DrawAssetSlot(asset, i, AssetManager.Assets.Get<Texture>(binding.AssetTexture), sw);
            else
                DrawAssetSlotEmptyTexture(asset, i, binding, sw);

            ImGui.PopID();
        }

        ImGui.EndTable();
    }


    private void DrawAssetSlot(Material material, int slot, Texture slotTexture, NativeSpanWriter sw)
    {
        var rowHeight = ImGui.GetFrameHeight();
        var clearBtnWidth = rowHeight + ImGui.GetStyle().ItemSpacing.X;
        var contentWidth = ImGui.GetContentRegionAvail().X - clearBtnWidth;

        if (ImGui.Button(sw.Write(slotTexture.Name), new Vector2(contentWidth, rowHeight)))
            ImGui.OpenPopup("preview-popup"u8);

        DropTexture(material, slot);

        ImGui.SameLine();

        if (slotTexture.Id.IsValid() && ImGui.Button("X"u8, new Vector2(rowHeight, rowHeight)))
            material.SetTextureSlot(slot, null);

        if (ImGui.IsItemHovered())
        {
            ImGui.BeginTooltip();
            ImGui.TextUnformatted("Clear Slot"u8);
            ImGui.EndTooltip();
        }

        if (ImGui.BeginPopup("##preview-popup"u8))
        {
            state.GetOrSetTextureHandle(slotTexture.GfxId, ref AssetInspectorPanel.PopupTextureHandle);
            ImGui.Image(AssetInspectorPanel.PopupTextureHandle, new Vector2(256, 256));

            if (ImGui.Button("Close"u8)) ImGui.CloseCurrentPopup();
            ImGui.EndPopup();
        }
    }

    private void DrawAssetSlotEmptyTexture(Material material, int slot, TextureSource source, NativeSpanWriter sw)
    {
        var size = new Vector2(ImGui.GetContentRegionAvail().X, ImGui.GetFrameHeight());

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
            if (droppedId.Value > 0 && AssetManager.Assets.TryGet<Texture>(droppedId, out var droppedTex))
                material.SetTextureSlot(slot, droppedTex);
        }

        ImGui.EndDragDropTarget();
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static void DrawFlagToggle(ReadOnlySpan<byte> label, GfxDrawFlags flag, ref GfxDrawState state,
        NativeSpanWriter sw)
    {
        var isDefined = state.IsSet(flag);
        if (ImGui.Checkbox(sw.Append(label).Append("##1-"u8).Append((int)flag).End(), ref isDefined))
            state = new GfxDrawState(state.Enabled, state.Defined ^ flag);

        if (!isDefined) return;

        ImGui.SameLine(130);

        var isEnabled = state.IsEnabled(flag);
        if (ImGui.Checkbox(sw.Append("##2-"u8).Append((int)flag).End(), ref isEnabled))
            state = new GfxDrawState(state.Enabled ^ flag, state.Defined);
    }
}