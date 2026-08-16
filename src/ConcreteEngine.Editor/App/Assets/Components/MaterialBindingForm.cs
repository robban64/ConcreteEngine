using System.Numerics;
using ConcreteEngine.Core.Engine.Assets;
using ConcreteEngine.Editor.App.Theme;
using ConcreteEngine.Editor.Data;
using ConcreteEngine.Graphics.Gfx;
using Hexa.NET.ImGui;

namespace ConcreteEngine.Editor.App.Assets.Components;

internal sealed unsafe class MaterialBindingForm
{
    const float ImageThumbnailSize = 64f;

    private readonly TexturePtrHandle[] _textureHandles = new TexturePtrHandle[16];

    private static float ImageOffset => ImageThumbnailSize + GuiTheme.FramePadding.X;

    public void Draw(Material material)
    {
        var span = material.GetSourceSpan();
        for (var i = 0; i < span.Length; i++)
        {
            var source = span[i];
            ImGui.PushID(i);

            ImGui.SeparatorText(source.Slot.ToUtf8());

            ImGui.BeginGroup();
            if (source.TextureId.IsValid())
            {
                var texture = AssetManager.Assets.Get<Texture>(source.AssetId);
                ImGui.AlignTextToFramePadding();
                AppDraw.Text(texture.Name);
                ImGui.AlignTextToFramePadding();
                ImGui.TextUnformatted(texture.TextureKind.ToUtf8());
            }
            else
            {
                ImGui.AlignTextToFramePadding();
                AppDraw.TextColored(Palette32.OrangeBase, "Fallback"u8);
            }
            ImGui.AlignTextToFramePadding();
            ImGui.TextUnformatted(source.Profile.ToUtf8());
            ImGui.EndGroup();

            var availWidth = ImGui.GetContentRegionAvail().X;
            ImGui.SameLine(availWidth - ImageOffset);

            ref var handle = ref  _textureHandles[(int)source.Slot];
            AppDraw.ImageButton("##x"u8, source.GetTextureOrFallback(), ref handle, new Vector2(ImageThumbnailSize));

            if (source.TextureId.IsValid() && ImGui.IsItemClicked(ImGuiMouseButton.Right))
                material.ClearSourceSlot(source.Slot);
            
            DropTexture(material, source.Slot);


            ImGui.PopID();
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