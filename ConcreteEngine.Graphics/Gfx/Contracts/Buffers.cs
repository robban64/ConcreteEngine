#region

using ConcreteEngine.Graphics.Gfx.Definitions;

#endregion

namespace ConcreteEngine.Graphics.Gfx.Contracts;

public readonly struct GfxBufferDataDesc(uint size, BufferStorage storage, BufferAccess access)
{
    public readonly uint Size = size;
    public readonly BufferStorage Storage = storage;
    public readonly BufferAccess Access = access;
}

//VBO
public readonly struct VertexBufferPayload(in VertexBufferDesc descriptor, ReadOnlyMemory<byte> data)
{
    public readonly VertexBufferDesc Descriptor = descriptor;
    public readonly ReadOnlyMemory<byte> Data = data;
}

public readonly struct VertexBufferDesc(
    int bindingIdx,
    int vertexSize, // stride
    int vertexCount,
    BufferUsage usage,
    BufferStorage storage,
    BufferAccess access
)
{
    public readonly int BindingIdx = bindingIdx;
    public readonly int VertexSize = vertexSize;
    public readonly int VertexCount = vertexCount;
    public readonly BufferUsage Usage = usage;
    public readonly BufferStorage Storage = storage;
    public readonly BufferAccess Access = access;
}

//IBO
public readonly struct IndexBufferPayload(in IndexBufferDesc descriptor, ReadOnlyMemory<byte> data)
{
    public readonly IndexBufferDesc Descriptor = descriptor;
    public readonly ReadOnlyMemory<byte> Data = data;
}

public readonly struct IndexBufferDesc(
    uint elementSize,
    uint elementCount,
    BufferUsage usage,
    BufferStorage storage,
    BufferAccess access)
{
    public readonly uint ElementSize = elementSize;
    public readonly uint ElementCount = elementCount;
    public readonly BufferUsage Usage = usage;
    public readonly BufferStorage Storage = storage;
    public readonly BufferAccess Access = access;
}

public readonly struct BufferDescriptor(
    BufferUsage usage,
    BufferStorage storage,
    BufferAccess access)
{
    public readonly BufferUsage Usage = usage;
    public readonly BufferStorage Storage = storage;
    public readonly BufferAccess Access = access;


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