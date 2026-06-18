using ConcreteEngine.Renderer.Core;

namespace ConcreteEngine.Core.Engine.Assets;

public readonly record struct TextureSource(
    AssetId AssetTexture,
    TextureUsage Usage,
    TextureId FallbackTexture,
    TextureId OverrideTexture = default
)
{
    public bool IsBound() => AssetTexture.IsValid() || OverrideTexture.IsValid();
    public TextureSource WithTexture(AssetId assetTexture, TextureId overrideTexture = default)
        => new (assetTexture, Usage, FallbackTexture, overrideTexture);
    
    public TextureSource WithAssetId(AssetId assetId) => this with { AssetTexture = assetId };

}