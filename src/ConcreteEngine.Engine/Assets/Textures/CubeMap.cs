using ConcreteEngine.Core.Specs.Graphics;
using ConcreteEngine.Engine.Metadata;
using ConcreteEngine.Graphics.Gfx.Handles;

namespace ConcreteEngine.Engine.Assets.Textures;

public sealed class CubeMap : AssetObject
{
    public AssetRef<CubeMap> RefId => new(RawId);

    public new required TextureId ResourceId { get; init; }
    public required int Size { get; init; }
    public override AssetKind Kind => AssetKind.TextureCubeMap;
    public override AssetCategory Category => AssetCategory.Graphic;
    public GraphicsHandleKind GraphicsKind => GraphicsHandleKind.Texture;
}