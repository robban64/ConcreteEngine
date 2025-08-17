using System.Numerics;
using ConcreteEngine.Graphics.Definitions;
using ConcreteEngine.Graphics.Resources;
using Silk.NET.Maths;

namespace ConcreteEngine.Graphics.OpenGL;

internal readonly record struct CreateGlFrameBufferResult(uint Fbo, uint Texture, uint Renderbuffer);

public sealed class GlFramebuffer : IFrameBuffer
{
    public uint Handle { get; }
    public uint RenderBufferHandle { get; }
    public ushort ColorTextureId { get; }
    public Vector2D<int> Size { get; set; }
    public Vector2 SizeRatio { get; set; }
    public bool IsDisposed { get; set; }

    public GlFramebuffer( uint handle, uint renderBufferHandle, ushort colorTextureId, Vector2D<int> size, Vector2 sizeRatio)
    {
        Handle = handle;
        RenderBufferHandle = renderBufferHandle;
        ColorTextureId = colorTextureId;
        Size = size;
        SizeRatio = sizeRatio;
    }
}