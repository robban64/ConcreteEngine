#region

using ConcreteEngine.Graphics.Gfx.Definitions;
using ConcreteEngine.Renderer.Definitions;

#endregion

namespace ConcreteEngine.Engine.Assets.Data;

public readonly struct AssetTextureSlot(
    AssetId asset,
    TextureSlotKind slotKind,
    TextureKind textureKind = TextureKind.Texture2D,
    TexturePixelFormat pixelFormat = TexturePixelFormat.SrgbAlpha
)
{
    public readonly AssetId Asset  = asset;
    public readonly TextureSlotKind SlotKind  = slotKind;
    public readonly TextureKind TextureKind  = textureKind;
    public readonly TexturePixelFormat PixelFormat  = pixelFormat;

    public bool IsFallback => !Asset.IsValid;

}