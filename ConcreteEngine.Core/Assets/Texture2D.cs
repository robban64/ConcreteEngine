#region

using ConcreteEngine.Graphics;

#endregion

namespace ConcreteEngine.Core.Assets;

public class Texture2D : IGraphicAssetFile
{
    public required string Name { get; init; }
    public required string Path { get; init; }
    public required IGraphicsResource GraphicsResource { get; init; }

    public AssetFileType AssetType => AssetFileType.Texture2D;
    public ITexture2D Texture => (GraphicsResource as ITexture2D)!;

    public int Width => Texture.Width;
    public int Height => Texture.Height;
}