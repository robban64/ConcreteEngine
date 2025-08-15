#region

using ConcreteEngine.Graphics.Definitions;
using ConcreteEngine.Graphics.Resources;
using ConcreteEngine.Graphics.Utils;
using Silk.NET.OpenGL;

#endregion

namespace ConcreteEngine.Graphics.OpenGL;

public sealed class GlIndexBuffer(uint handle, BufferUsage usage)
    : GlBuffer(handle, BufferTarget.IndexBuffer, usage);

public sealed class GlVertexBuffer(uint handle, BufferUsage usage)
    : GlBuffer(handle, BufferTarget.VertexBuffer, usage);

public abstract class GlBuffer : IGraphicsBuffer
{
    public uint Handle { get; }
    public bool IsDisposed { get; set; } = false;

    public BufferUsage Usage { get; }
    public BufferUsageARB GlBufferUsage { get; }

    public BufferTarget Target { get; }
    public BufferTargetARB GlBufferTarget { get; }

    public int ElementCount { get; internal set; }
    public int ElementSize { get; internal set; }
    public int BufferSizeInBytes => ElementCount * ElementSize;
    public bool IsStatic => Usage == BufferUsage.StaticDraw;

    internal GlBuffer(uint handle, BufferTarget target, BufferUsage usage) 
    {
        Handle = handle;
        Usage = usage;
        GlBufferUsage = usage.ToGlEnum();

        Target = target;
        GlBufferTarget = target.ToGlEnum();
    }
}