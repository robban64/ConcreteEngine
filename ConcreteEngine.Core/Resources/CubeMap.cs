using ConcreteEngine.Core.Assets;
using ConcreteEngine.Graphics;
using ConcreteEngine.Graphics.Resources;

namespace ConcreteEngine.Core.Resources;

public sealed class CubeMap : IGraphicAssetFile<TextureId>, IResourceTexture
{
    public required string Name { get; init; }
    public required TextureId ResourceId { get; init; }
    public required string[] Textures { get; init; }
    public required int Width { get; init; }
    public required int Height { get; init; }
    public required EnginePixelFormat PixelFormat { get; init; }
    
    public AssetFileType AssetType => AssetFileType.Cubemap;
}