namespace ConcreteEngine.Graphics.Contracts;

public sealed class IndexBufferPayload
{
    public required IndexBufferDesc Descriptor { get; init;}
    public required ReadOnlyMemory<byte>? Data { get; init; }
}

public readonly record struct IndexBufferDesc(
    uint ElementSize,
    uint ElementCount,
    BufferUsage Usage,
    BufferStorage Storage,
    BufferAccess Access);