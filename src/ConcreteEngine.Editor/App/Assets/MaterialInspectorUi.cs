using System.Numerics;
using System.Runtime.CompilerServices;
using ConcreteEngine.Core.Engine.Assets;
using ConcreteEngine.Core.Engine.Graphics;
using ConcreteEngine.Editor.App.Inspectors;
using ConcreteEngine.Editor.App.Theme;
using ConcreteEngine.Editor.Core;
using ConcreteEngine.Editor.Data;
using ConcreteEngine.Editor.Utils;
using ConcreteEngine.Graphics.Gfx;
using Hexa.NET.ImGui;

namespace ConcreteEngine.Editor.App.Assets;

internal sealed unsafe class MaterialInspectorUi(StateManager state)
{
    private readonly MaterialInspector _inspector = new();

    public void Draw(Material material)
    {
        ImGui.SeparatorText("Material Info"u8);
        ImGui.BeginGroup();
        ImGui.TextUnformatted("Shader: "u8);
        ImGui.SameLine();
        AppDraw.TextColored(Palette32.White, material.BoundShader.Name);
        ImGui.EndGroup();

        ImGui.Spacing();
        ImGui.SeparatorText("Texture Slots"u8);
        DrawTextureSlots(material);

        _inspector.DrawMaterialState();

        ImGui.SeparatorText("Render Properties"u8);
        DrawPipeline(material);
        ImGui.Spacing();

        _inspector.DrawPipeline();
    }


    private void DrawTextureSlots(Material asset)
    {
        var rowHeight = ImGui.GetFrameHeight();
        var offset = ImGui.GetContentRegionAvail().X * 0.33f + GuiTheme.ItemSpacing.X;

        var usageNames = TextureUsageExt.Names;
        var bindings = asset.GetSourceSpan();
        for (var i = 0; i < bindings.Length; i++)
        {
            var binding = bindings[i];
            ImGui.PushID(i);
            AppDraw.Text(usageNames[(int)binding.Usage]);
            ImGui.SameLine(offset);
            if (binding.AssetId.IsValid())
                DrawAssetSlot(asset, i, AssetManager.Assets.Get<Texture>(binding.AssetId), rowHeight);
            else
                DrawAssetSlotEmptyTexture(asset, i, rowHeight);

            ImGui.PopID();
        }
    }


    private void DrawPipeline(Material material)
    {
        var ogDrawState = material.State.DrawState;
        var drawState = material.State.DrawState;
        DrawFlagToggle("Blend Mode"u8, GfxDrawFlags.Blend, ref drawState);
        DrawFlagToggle("Cull Mode"u8, GfxDrawFlags.Cull, ref drawState);
        DrawFlagToggle("Depth Test"u8, GfxDrawFlags.DepthTest, ref drawState);
        DrawFlagToggle("Depth Write"u8, GfxDrawFlags.DepthWrite, ref drawState);
        DrawFlagToggle("Polygon Offset"u8, GfxDrawFlags.PolygonOffset, ref drawState);
        ImGui.Separator();
        DrawFlagToggle("A2C"u8, GfxDrawFlags.Ac2, ref drawState);

        if (ogDrawState != drawState)
            material.State.DrawState = drawState;

        if (drawState.IsEmpty()) return;
/*
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
        */
    }

    private void DrawAssetSlot(Material material, int slot, Texture slotTexture, float rowHeight)
    {
        var clearBtnWidth = rowHeight + GuiTheme.ItemSpacing.X;
        var contentWidth = ImGui.GetContentRegionAvail().X - clearBtnWidth;

        var name = ScratchBuffer.Writer().Write(slotTexture.Name);
        if (ImGui.Button(name, new Vector2(contentWidth, rowHeight)))
            ImGui.OpenPopup("preview-popup"u8);

        DropTexture(material, slot);

        ImGui.SameLine();

        if (slotTexture.Id.IsValid() && ImGui.Button("X"u8, new Vector2(rowHeight, rowHeight)))
            material.ClearSourceSlot(slot);

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

    private void DrawAssetSlotEmptyTexture(Material material, int slot, float rowHeight)
    {
        ImGui.PushStyleColor(ImGuiCol.Text, Palette32.OrangeBase);
        ImGui.Button("Empty Slot"u8, new Vector2(ImGui.GetContentRegionAvail().X, rowHeight));
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
            if (droppedId.Id > 0 && AssetManager.Assets.TryGet<Texture>(droppedId, out var droppedTex))
                material.SetSourceSlot(slot, droppedTex);
        }

        ImGui.EndDragDropTarget();
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static void DrawFlagToggle(ReadOnlySpan<byte> label, GfxDrawFlags flag, ref GfxDrawState state)
    {
        var isDefined = state.IsSet(flag);

        var sw = ScratchBuffer.Writer();
        sw.Append(label);
        if (ImGui.Checkbox(sw.AppendImGuiId(1).Append((byte)flag).End(), ref isDefined))
            state = new GfxDrawState(state.Enabled, state.Defined ^ flag);

        if (!isDefined) return;

        ImGui.SameLine(130);

        var isEnabled = state.IsEnabled(flag);
        if (ImGui.Checkbox(sw.AppendImGuiId(2).Append((byte)flag).End(), ref isEnabled))
            state = new GfxDrawState(state.Enabled ^ flag, state.Defined);
    }
}