#region

using ConcreteEngine.Graphics.Definitions;

#endregion

namespace ConcreteEngine.Graphics.OpenGL;

public sealed class GlTexture2D : OpenGLResource, ITexture2D
{
    public int Width { get; }
    public int Height { get; }
    public EnginePixelFormat Format { get; }

    public GlTexture2D(uint handle, int width, int height, EnginePixelFormat format) : base(handle)
    {
        Width = width;
        Height = height;
        Format = format;
    }
}