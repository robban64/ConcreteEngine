namespace ConcreteEngine.Graphics.Contracts;

public sealed class VertexBufferPayload
{
    public required VertexBufferDesc Descriptor { get; init;}
    public required ReadOnlyMemory<byte>? Data { get; init; }
    
}

public readonly record struct VertexBufferDesc(
    uint BindingIdx,
    uint VertexSize,
    uint VertexCount,
    BufferUsage Usage,
    BufferStorage Storage,
    BufferAccess Access);