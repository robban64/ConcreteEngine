using System.Numerics;
using ConcreteEngine.Graphics.Data;
using Silk.NET.Maths;

namespace ConcreteEngine.Graphics.Resources;

public interface IFrameBuffer : IGraphicsResource
{
    public uint Handle { get; }
    public ushort ColTextureId { get; } // 0 if Msaa

    public uint RboHandle { get; }
    public uint RboTextureHandle { get; } // >0 if msaa
    public Vector2D<int> Size { get; set; }
    public Vector2 SizeRatio { get; set; }
    public bool IsDisposed { get; set; }
    
    public FramebufferDescriptor  Descriptor { get; set; }
}