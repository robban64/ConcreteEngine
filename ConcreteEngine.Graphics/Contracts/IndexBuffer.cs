namespace ConcreteEngine.Graphics.Contracts;

public readonly struct IndexBufferPayload(in IndexBufferDesc descriptor, ReadOnlyMemory<byte> data)
{
    public readonly IndexBufferDesc Descriptor = descriptor;
    public readonly ReadOnlyMemory<byte> Data = data;
}

public readonly record struct IndexBufferDesc(
    uint ElementSize,
    uint ElementCount,
    BufferUsage Usage,
    BufferStorage Storage,
    BufferAccess Access);