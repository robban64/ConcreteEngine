#region

using ConcreteEngine.Graphics.Definitions;

#endregion

namespace ConcreteEngine.Graphics;

public interface ITexture2D : IGraphicsResource
{
    public int Width { get; }
    public int Height { get; }
    public EnginePixelFormat Format { get; }
}