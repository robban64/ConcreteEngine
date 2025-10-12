#region

using ConcreteEngine.Graphics;
using ConcreteEngine.Graphics.Gfx.Definitions;
using ConcreteEngine.Graphics.Gfx.Resources;

#endregion

namespace ConcreteEngine.Core.Assets.Resources;

public interface ITextureResource
{
    public string Name { get; }
    public TextureId ResourceId { get; }
    public int Width { get; }
    public int Height { get; }
    public TexturePixelFormat PixelFormat { get; }
}

public sealed class Texture2D : IGraphicAssetFile<TextureId>, ITextureResource
{
    internal Texture2D()
    {
    }

    public required string Name { get; init; }
    public required string Path { get; init; }
    public required TextureId ResourceId { get; init; }
    public required int Width { get; init; }
    public required int Height { get; init; }
    public required TexturePixelFormat PixelFormat { get; init; }
    public TexturePreset Preset { get; init; }
    public TextureAnisotropy Anisotropy { get; init; }
    public AssetKind Kind => AssetKind.Texture2D;
    public ResourceKind GfxResourceKind => ResourceKind.Texture;

    private byte[]? _pixelData;
    public ReadOnlyMemory<byte>? PixelData => _pixelData?.AsMemory();
    internal void SetPixelData(byte[] pixelData) => _pixelData = pixelData;
}