using ConcreteEngine.Core.Renderer.Material;
using ConcreteEngine.Graphics.Gfx.Definitions;

namespace ConcreteEngine.Core.Engine.Assets.Data;

public readonly record struct TextureSource(
    AssetId Texture,
    TextureUsage Usage,
    TextureKind TextureKind = TextureKind.Texture2D,
    TexturePixelFormat PixelFormat = TexturePixelFormat.SrgbAlpha
)
{
    public readonly bool IsFallback = !Texture.IsValid() && HasFallbackArgs(Usage, TextureKind);

    public TextureSource WithAssetId(AssetId assetId) => this with { Texture = assetId };

    public string GetFallbackName()
    {
        if(TextureKind == TextureKind.Multisample2D)
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
        var fallBackUsage = usage switch
        {
            TextureUsage.Shadowmap => true,
            TextureUsage.Lightmap => true,
            _ => false
        };
        return fallBackUsage || textureKind == TextureKind.Multisample2D;
    }
    
}