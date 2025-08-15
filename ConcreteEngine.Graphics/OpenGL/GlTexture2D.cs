#region

using ConcreteEngine.Graphics.Definitions;
using ConcreteEngine.Graphics.Resources;

#endregion

namespace ConcreteEngine.Graphics.OpenGL;

public sealed class GlTexture2D : ITexture2D
{
    public uint Handle { get; }
    public bool IsDisposed { get; set; } = false;
    public int Width { get; }
    public int Height { get; }
    public EnginePixelFormat Format { get; }

    public GlTexture2D(uint handle, int width, int height, EnginePixelFormat format)
    {
        Handle = handle;
        Width = width;
        Height = height;
        Format = format;
    }
}