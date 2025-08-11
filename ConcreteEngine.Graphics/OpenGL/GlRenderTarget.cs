#region

using System.Drawing;
using Silk.NET.Maths;
using Silk.NET.OpenGL;

#endregion

namespace ConcreteEngine.Graphics.OpenGL;

public sealed class GlRenderTarget : OpenGLResource, IRenderTarget
{
    public ITexture2D? Texture { get; private set; }
    public Rectangle<int> ViewportSize { get; set; }
    private Rectangle<int> _lastViewportSize = new Rectangle<int>(0, 0, 0, 0);

    private GL Gl { get; }

    internal GlRenderTarget(GlGraphicsContext ctx, ITexture2D? texture = null) : base(0)
    {
        Gl = ctx.Gl;
        Texture = texture;
    }

    public void Bind()
    {
        _lastViewportSize = ViewportSize;

        Gl.BindFramebuffer(FramebufferTarget.Framebuffer, Handle);
        Gl.Viewport(_lastViewportSize);
    }

    public void Unbind()
    {
        Gl.BindFramebuffer(FramebufferTarget.Framebuffer, 0);
    }

    public void Clear(Color color)
    {
        Gl.ClearColor(color);
        Gl.Clear(ClearBufferMask.ColorBufferBit | ClearBufferMask.DepthBufferBit);
    }
}