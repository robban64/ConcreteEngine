namespace ConcreteEngine.Graphics.Contracts;

public readonly struct VertexBufferPayload(in VertexBufferDesc descriptor, ReadOnlyMemory<byte> data)
{
    public readonly VertexBufferDesc Descriptor = descriptor;
    public readonly ReadOnlyMemory<byte> Data = data;
}

public readonly record struct VertexBufferDesc(
    int BindingIdx,
    int VertexSize, // stride
    int VertexCount,
    BufferUsage Usage,
    BufferStorage Storage,
    BufferAccess Access
);