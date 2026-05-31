using ConcreteEngine.Graphics.Gfx;
using ConcreteEngine.Renderer.Core;

namespace ConcreteEngine.Core.Engine.Assets.Data;

public readonly record struct TextureSource(
    AssetId AssetTexture,
    TextureUsage Usage,
    TextureKind TextureKind = TextureKind.Texture2D,
    TexturePixelFormat PixelFormat = TexturePixelFormat.SrgbAlpha,
    TextureId OverrideTextureId = default
)
{
    public readonly bool IsFallback = !AssetTexture.IsValid() && HasFallbackArgs(TextureKind);

    public TextureSource WithAssetId(AssetId assetId) => this with { AssetTexture = assetId };

    public string GetFallbackName()
    {
        if (TextureKind == TextureKind.Multisample2D)
            return nameof(TextureKind.Multisample2D);

        return "Unknown";
    }

    private static bool HasFallbackArgs(TextureKind textureKind)
    {
        return textureKind == TextureKind.Multisample2D;
    }
}