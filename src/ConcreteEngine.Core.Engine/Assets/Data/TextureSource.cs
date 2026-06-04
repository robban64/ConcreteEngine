using ConcreteEngine.Graphics.Gfx;
using ConcreteEngine.Renderer.Core;

namespace ConcreteEngine.Core.Engine.Assets.Data;

public readonly record struct TextureSource(
    AssetId AssetTexture,
    TextureUsage Usage,
    TextureId OverrideTextureId = default
)
{
    public TextureSource WithAssetId(AssetId assetId) => this with { AssetTexture = assetId };

}