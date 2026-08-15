using System.Numerics;
using ConcreteEngine.Core.Engine.Assets;
using ConcreteEngine.Editor.App.Theme;
using ConcreteEngine.Editor.Data;
using ConcreteEngine.Graphics.Gfx;
using Hexa.NET.ImGui;

namespace ConcreteEngine.Editor.App.Assets;

internal sealed unsafe class MaterialBindingForm
{
    private readonly TexturePtrHandle[] _textureHandles = new TexturePtrHandle[16];

    public void Draw(Material material)
    {
        const float minInputBoxWidth = 100f;

        var availWidth = ImGui.GetContentRegionAvail().X;
        var rowHeight = ImGui.GetFrameHeight();

        float rightSpace = availWidth - (minInputBoxWidth + GuiTheme.ItemSpacing.X);
        float imageSize = float.Clamp(rightSpace, 24f, 64f);
        float inputFrameWidth = availWidth - imageSize - GuiTheme.ItemSpacing.X;

        foreach (var source in material.GetSourceSpan())
        {
            ImGui.PushID((int)source.Slot);

            DrawTitle(source);
            
            DrawUvInputGroup(inputFrameWidth);
            ImGui.SameLine();
            DrawTextureGroup(material, source, rowHeight, imageSize);
            
            ImGui.PopID();
        }
    }

    private static void DrawTitle(TextureSource source)
    {
        var sw = ScratchBuffer.Writer();
        sw.Append(SamplerSlotExt.Names[(int)source.Slot]).PadRight(2);
        sw.Append(SamplerProfileExt.Names[(int)source.Profile]);
        ImGui.AlignTextToFramePadding();
        AppDraw.Text(sw.End());
    }

    private static void DrawUvInputGroup(float width)
    {
        ImGui.BeginGroup();
        {
            Vector2 v = default;

            ImGui.SetNextItemWidth(width);
            ImGui.AlignTextToFramePadding();
            ImGui.TextUnformatted("Offset"u8);
            ImGui.InputFloat2("##Offset"u8, ref v.X);

            ImGui.SetNextItemWidth(width);
            ImGui.AlignTextToFramePadding();
            ImGui.TextUnformatted("Repeat"u8);
            ImGui.InputFloat2("##Repeat"u8, ref v.X);
        }
        ImGui.EndGroup();
    }

    private void DrawTextureGroup(Material material, TextureSource source, float rowHeight, float imageSize)
    {
        ImGui.BeginGroup();
        ImGui.Dummy(new Vector2(0, rowHeight));
        if (source.TextureId.IsValid())
        {
            AppDraw.ImageButton("##text-slot"u8, source.TextureId, ref _textureHandles[(int)source.Slot],
                new Vector2(imageSize));
        }
        else
        {
            ImGui.PushStyleColor(ImGuiCol.Text, Palette32.OrangeBase);
            ImGui.Button("Empty Slot"u8, new Vector2(imageSize) + GuiTheme.FramePadding);
            ImGui.PopStyleColor();
        }

        ImGui.EndGroup();

        if (source.TextureId.IsValid() && ImGui.IsItemClicked(ImGuiMouseButton.Right))
            material.ClearSourceSlot(source.Slot);

        DropTexture(material, source.Slot);

        if (ImGui.IsItemHovered())
        {
            ImGui.BeginTooltip();
            ImGui.TextUnformatted("Right + Click to clear slot"u8);
            ImGui.EndTooltip();
        }
    }
    
    private static void DropTexture(Material material, SamplerSlot slot)
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
}