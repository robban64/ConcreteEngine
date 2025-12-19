using ConcreteEngine.Engine.Assets.Data;
using ConcreteEngine.Graphics.Gfx.Definitions;
using ConcreteEngine.Graphics.Gfx.Resources;

namespace ConcreteEngine.Engine.Assets.Textures;

public sealed class CubeMap : AssetObject
{
    public AssetRef<CubeMap> RefId => new(RawId);

    public new required TextureId ResourceId { get; init; }
    public required int Size { get; init; }
    public override AssetKind Kind => AssetKind.TextureCubeMap;
    public override AssetCategory Category => AssetCategory.Graphic;
    public ResourceKind GfxResourceKind => ResourceKind.Texture;
}