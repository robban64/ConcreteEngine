#region

using ConcreteEngine.Graphics;
using ConcreteEngine.Graphics.Gfx.Definitions;
using ConcreteEngine.Graphics.Gfx.Resources;

#endregion

namespace ConcreteEngine.Core.Assets.Resources;

public sealed class CubeMap : IGraphicAssetFile<TextureId>, ITextureResource
{
    public required string Name { get; init; }
    public required TextureId ResourceId { get; init; }
    public required string[] Textures { get; init; }
    public required int Width { get; init; }
    public required int Height { get; init; }
    public required TexturePixelFormat PixelFormat { get; init; }

    public AssetKind Kind => AssetKind.CubeMap;
    public ResourceKind GfxResourceKind => ResourceKind.Texture;
}