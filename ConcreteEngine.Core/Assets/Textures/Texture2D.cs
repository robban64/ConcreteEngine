#region

using ConcreteEngine.Core.Assets.Data;
using ConcreteEngine.Graphics.Gfx.Definitions;
using ConcreteEngine.Graphics.Gfx.Resources;

#endregion

namespace ConcreteEngine.Core.Assets.Textures;

public sealed class Texture2D : AssetObject
{
    public AssetRef<Texture2D> RefId => new(RawId);
    
    public required TextureId ResourceId { get; init; }

    public required int Width { get; init; }
    public required int Height { get; init; }

    
    public override AssetCategory Category => AssetCategory.Graphic;
    public override AssetKind Kind =>  AssetKind.Texture2D;
    public ResourceKind GfxResourceKind => ResourceKind.Texture;

    private byte[]? _pixelData;
    public ReadOnlyMemory<byte>? PixelData => _pixelData?.AsMemory();
    internal void SetPixelData(byte[] pixelData) => _pixelData = pixelData;
}