using System.Runtime.CompilerServices;
using ConcreteEngine.Graphics.Gfx.Definitions;
using ConcreteEngine.Graphics.Handles;
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
    public readonly bool IsFallback = !AssetTexture.IsValid() && HasFallbackArgs(Usage, TextureKind);

    public TextureSource WithAssetId(AssetId assetId) => this with { AssetTexture = assetId };

    public string GetFallbackName()
    {
        if (TextureKind == TextureKind.Multisample2D)
            return nameof(TextureKind.Multisample2D);

        return Usage switch
        {
            TextureUsage.Shadowmap => nameof(TextureUsage.Shadowmap),
            TextureUsage.Lightmap => nameof(TextureUsage.Lightmap),
            _ => "Unknown"
        };
    }

    private static bool HasFallbackArgs(TextureUsage usage, TextureKind textureKind)
    {
        return usage is TextureUsage.Shadowmap or TextureUsage.Lightmap || textureKind == TextureKind.Multisample2D;
    }
}