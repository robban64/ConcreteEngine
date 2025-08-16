#region

using System.Drawing;
using ConcreteEngine.Graphics.Resources;
using Silk.NET.Maths;
using Silk.NET.OpenGL;

#endregion

namespace ConcreteEngine.Graphics.OpenGL;

public sealed class GlRenderTarget :  IRenderTarget
{
    public uint Handle { get; }

    public bool IsDisposed { get; set; }

    public ITexture2D? Texture { get; private set; }
    public Rectangle<int> ViewportSize { get; set; }

    private GL Gl { get; }

    internal GlRenderTarget(GlGraphicsContext gfx, ITexture2D? texture = null)
    {
        Handle = 0;
        Gl = gfx.Gl;
        Texture = texture;
    }

}