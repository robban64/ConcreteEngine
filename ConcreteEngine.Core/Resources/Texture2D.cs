#region

using ConcreteEngine.Core.Assets;
using ConcreteEngine.Graphics;
using ConcreteEngine.Graphics.Resources;

#endregion

namespace ConcreteEngine.Core.Resources;

public interface IResourceTexture
{
    public string Name { get;  }
    public TextureId ResourceId { get;  }
    public int Width { get;  }
    public int Height { get;  }
    public EnginePixelFormat PixelFormat { get;  }

}
public sealed class Texture2D : IGraphicAssetFile<TextureId>, IResourceTexture
{
    internal Texture2D(){}

    public required string Name { get; init; }
    public required string Path { get; init; }
    public required TextureId ResourceId { get; init; }
    public required int Width { get; init; }
    public required int Height { get; init; }
    public required EnginePixelFormat PixelFormat { get; init; }
    public TexturePreset Preset { get; init; }
    public TextureAnisotropy Anisotropy  { get; init; }
    public AssetKind AssetType => AssetKind.Texture2D;
    public ResourceKind GfxResourceKind => ResourceKind.Texture;
    
    private byte[]? _pixelData;
    public ReadOnlyMemory<byte>? PixelData => _pixelData?.AsMemory();
    internal void SetPixelData(byte[] pixelData) =>  _pixelData = pixelData;

}