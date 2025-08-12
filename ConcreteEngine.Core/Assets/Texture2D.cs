#region

using ConcreteEngine.Graphics;
using ConcreteEngine.Graphics.Definitions;

#endregion

namespace ConcreteEngine.Core.Assets;

public class Texture2D : IGraphicAssetFile
{
    public required string Name { get; init; }
    public required int Width { get; init; }
    public required int Height { get; init; }
    public required EnginePixelFormat  PixelFormat { get; init; }
    public required string Path { get; init; }
    public required int ResourceId { get; init; }

    public AssetFileType AssetType => AssetFileType.Texture2D;

}