using System.Numerics;
using ConcreteEngine.Graphics.Data;
using ConcreteEngine.Graphics.Definitions;
using ConcreteEngine.Graphics.Resources;
using Silk.NET.Maths;

namespace ConcreteEngine.Graphics.OpenGL;

internal readonly record struct CreateGlFrameBufferResult(
    uint Fbo,
    uint Texture,
    uint Rbo,
    uint RboTexture,
    Vector2D<int> Size);

public sealed class GlFramebuffer : IFrameBuffer
{
    public uint Handle { get; }
    public ushort ColTextureId { get; } // 0 if Msaa

    public uint RboHandle { get; }
    public uint RboTextureHandle { get; } // >0 if msaa
    public Vector2D<int> Size { get; set; }
    public Vector2 SizeRatio { get; set; }
    public bool IsDisposed { get; set; }
    
    public FramebufferDescriptor  Descriptor { get; set; }

    public GlFramebuffer(uint handle, ushort colTextureId, uint rboHandle, uint rboTexHandle, Vector2D<int> size,
        Vector2 sizeRatio, in FramebufferDescriptor descriptor)
    {
        Handle = handle;
        ColTextureId = colTextureId;
        RboHandle = rboHandle;
        RboTextureHandle = rboTexHandle;
        Size = size;
        SizeRatio = sizeRatio;
        Descriptor = descriptor;
    }
}