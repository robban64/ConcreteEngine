using ConcreteEngine.Core.Renderer.Material;
using ConcreteEngine.Graphics.Gfx.Definitions;

namespace ConcreteEngine.Core.Engine.Assets.Data;

public readonly struct TextureSource(
    AssetId texture,
    TextureUsage usage,
    TextureKind textureKind = TextureKind.Texture2D,
    TexturePixelFormat pixelFormat = TexturePixelFormat.SrgbAlpha
)
{
    public readonly AssetId Texture = texture;
    public readonly TextureUsage Usage = usage;
    public readonly TextureKind TextureKind = textureKind;
    public readonly TexturePixelFormat PixelFormat = pixelFormat;
    public readonly bool IsFallback = !texture.IsValid() && !HasFallbackArgs(usage, textureKind);

    public TextureSource WithAssetId(AssetId assetId) => new(assetId, Usage, TextureKind, PixelFormat);

    private static bool HasFallbackArgs(TextureUsage usage, TextureKind textureKind)
    {
        var fallBackUsage = usage switch
        {
            TextureUsage.Shadowmap => true,
            TextureUsage.Lightmap => true,
            _ => false
        };
        return fallBackUsage || textureKind == TextureKind.Multisample2D;
    }
}