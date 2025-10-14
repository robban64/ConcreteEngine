#region

using ConcreteEngine.Core.Assets.Data;
using ConcreteEngine.Graphics.Gfx.Definitions;
using ConcreteEngine.Graphics.Gfx.Resources;

#endregion

namespace ConcreteEngine.Core.Assets.Textures;

public sealed class CubeMap : AssetObject
{
    public AssetRef<CubeMap> RefId => new(RawId);
    
    public required TextureId ResourceId { get; init; }
    public required int Size { get; init; }
    public override AssetKind Kind => AssetKind.TextureCubeMap;
    public override AssetCategory Category => AssetCategory.Graphic;
    public ResourceKind GfxResourceKind => ResourceKind.Texture;
}