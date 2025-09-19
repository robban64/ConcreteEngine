namespace ConcreteEngine.Graphics.Contracts;

internal sealed class FrameBuffer
{
    
}

public readonly record struct GfxBufferDataDesc(nint Size, BufferStorage Storage, BufferAccess Access);