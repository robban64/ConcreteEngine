using ConcreteEngine.Core.Renderer.Material;
using ConcreteEngine.Graphics.Gfx.Definitions;

namespace ConcreteEngine.Core.Engine.Assets;

public readonly struct AssetTextureSlot(
    AssetId asset,
    MaterialSlotKind slotKind,
    TextureKind textureKind = TextureKind.Texture2D,
    TexturePixelFormat pixelFormat = TexturePixelFormat.SrgbAlpha
)
{
    public readonly AssetId Asset = asset;
    public readonly MaterialSlotKind SlotKind = slotKind;
    public readonly TextureKind TextureKind = textureKind;
    public readonly TexturePixelFormat PixelFormat = pixelFormat;

    public bool IsFallback => !Asset.IsValid();

    public AssetTextureSlot WithAssetId(AssetId assetId) => new(assetId, SlotKind, TextureKind, PixelFormat);
}