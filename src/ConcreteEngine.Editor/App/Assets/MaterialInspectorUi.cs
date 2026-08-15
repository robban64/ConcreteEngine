using System.Numerics;
using System.Runtime.CompilerServices;
using ConcreteEngine.Core.Common.Text;
using ConcreteEngine.Core.Diagnostics.Time;
using ConcreteEngine.Core.Engine.Assets;
using ConcreteEngine.Editor.App.Inspectors;
using ConcreteEngine.Editor.App.Theme;
using ConcreteEngine.Editor.App.UI;
using ConcreteEngine.Editor.Core;
using ConcreteEngine.Editor.Data;
using ConcreteEngine.Editor.Utils;
using ConcreteEngine.Graphics.Gfx;
using Hexa.NET.ImGui;

namespace ConcreteEngine.Editor.App.Assets;

internal sealed unsafe class MaterialInspectorUi(StateManager state)
{
    private readonly MaterialInspector _inspector = new();

    private readonly TexturePtrHandle[] _textureHandles = new TexturePtrHandle[16];
    private readonly MaterialBindingForm _bindingForm = new();

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
        avg.BeginSample();
        _bindingForm.Draw(material);
        //DrawTextureSlots(material);
        if (avg.EndSample() > 40) avg.ResetAndPrint();

        _inspector.DrawMaterialState();

        ImGui.SeparatorText("Render Properties"u8);
        DrawPipeline(material);
        ImGui.Spacing();

        _inspector.DrawPipeline();
    }

    private AvgFrameTimer avg;


    private void DrawTextureSlots(Material asset)
    {
        const float minInputBoxWidth = 100f;

        var availWidth = ImGui.GetContentRegionAvail().X;
        var rowHeight = ImGui.GetFrameHeight();
        
        float rightSpace = availWidth - (minInputBoxWidth + GuiTheme.ItemSpacing.X);
        float imageSize = float.Clamp(rightSpace, 24f, 64f);
        float inputFrameWidth = availWidth - imageSize - GuiTheme.ItemSpacing.X;

        foreach (var binding in asset.GetSourceSpan())
        {
            ImGui.PushID((int)binding.Slot);

            var sw = ScratchBuffer.Writer();
            sw.Append(SamplerSlotExt.Names[(int)binding.Slot]).PadRight(2);
            sw.Append(SamplerProfileExt.Names[(int)binding.Profile]);
            ImGui.AlignTextToFramePadding();
            AppDraw.Text(sw.End());

            DrawAssetSlot(asset, binding, rowHeight, imageSize, inputFrameWidth);

            ImGui.PopID();
        }
    }

    private void DrawAssetSlot(Material material, TextureSource source, float rowHeight, float imageSize, float inputFrameWidth)
    {
        var texture = source.AssetId.IsValid() ? AssetManager.Assets.Get<Texture>(source.AssetId) : null;

        ImGui.BeginGroup();
        {
            Vector2 v = default;
            
            ImGui.SetNextItemWidth(inputFrameWidth);
            ImGui.AlignTextToFramePadding();
            ImGui.TextUnformatted("Offset"u8);
            ImGui.InputFloat2("##Offset"u8, ref v.X);
            
            ImGui.SetNextItemWidth(inputFrameWidth);
            ImGui.AlignTextToFramePadding();
            ImGui.TextUnformatted("Repeat"u8);
            ImGui.InputFloat2("##Repeat"u8, ref v.X);
        }
        ImGui.EndGroup();

        ImGui.SameLine();
        
        ImGui.BeginGroup();
        ImGui.Dummy(new Vector2(0,rowHeight));

        if (texture is not null)
        {
            AppDraw.ImageButton("##text-slot"u8, texture.GfxId, ref _textureHandles[(int)source.Slot], new Vector2(imageSize));
        }
        else
        {
            ImGui.PushStyleColor(ImGuiCol.Text, Palette32.OrangeBase);
            ImGui.Button("Empty Slot"u8, new Vector2(imageSize) + GuiTheme.FramePadding);
            ImGui.PopStyleColor();
        }
        ImGui.EndGroup();

        if (texture != null && ImGui.IsItemClicked(ImGuiMouseButton.Right))
            material.ClearSourceSlot(source.Slot);

        DropTexture(material, source.Slot);

        if (ImGui.IsItemHovered())
        {
            ImGui.BeginTooltip();
            ImGui.TextUnformatted("Right + Click to clear slot"u8);
            ImGui.EndTooltip();
        }

    }

    private void DropTexture(Material material, SamplerSlot slot)
    {
        if (!ImGui.BeginDragDropTarget()) return;

        var payload = ImGui.AcceptDragDropPayload("ASSET_TEXTURE"u8);
        if (!payload.IsNull && payload.IsDelivery())
        {
            var droppedId = *(AssetId*)payload.Data;
            if (droppedId.Id > 0 && AssetManager.Assets.TryGet<Texture>(droppedId, out var droppedTex))
                material.SetSourceSlot(droppedTex, slot);
        }

        ImGui.EndDragDropTarget();
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