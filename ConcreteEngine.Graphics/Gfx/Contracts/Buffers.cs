namespace ConcreteEngine.Graphics.Gfx.Contracts;

public readonly record struct GfxBufferDataDesc(nint Size, BufferStorage Storage, BufferAccess Access);

//VBO
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

//IBO
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

public readonly record struct BufferDescriptor(
    BufferUsage Usage,
    BufferStorage Storage,
    BufferAccess Access)
{
    public static BufferDescriptor MakeStatic() => new(BufferUsage.StaticDraw, BufferStorage.Static, BufferAccess.None);

    public static BufferDescriptor MakeDynamic() =>
        new(BufferUsage.DynamicDraw, BufferStorage.Dynamic, BufferAccess.None);

    public static BufferDescriptor MakeStream() => new(BufferUsage.StreamDraw, BufferStorage.Stream, BufferAccess.None);

    public static BufferDescriptor MakeMappedReadOnly() =>
        new(BufferUsage.StaticDraw, BufferStorage.Static, BufferAccess.MapRead);

    public static BufferDescriptor MakeMappedWriteOnly() =>
        new(BufferUsage.DynamicDraw, BufferStorage.Dynamic, BufferAccess.MapWrite);

    public static BufferDescriptor MakePersistentMapped() =>
        new(BufferUsage.StreamDraw, BufferStorage.Stream,
            BufferAccess.MapRead | BufferAccess.MapWrite | BufferAccess.Persistent | BufferAccess.Coherent);
}