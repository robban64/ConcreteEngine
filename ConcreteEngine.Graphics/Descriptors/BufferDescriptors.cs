namespace ConcreteEngine.Graphics.Descriptors;

public readonly record struct BufferDescriptor(
    BufferUsage Usage,
    BufferStorage Storage,
    BufferAccess Access)
{
    public static BufferDescriptor MakeStatic() =>
        new(BufferUsage.StaticDraw, BufferStorage.Static, BufferAccess.None);

    public static BufferDescriptor MakeDynamic() =>
        new(BufferUsage.DynamicDraw, BufferStorage.Dynamic, BufferAccess.None);

    public static BufferDescriptor MakeStream() =>
        new(BufferUsage.StreamDraw, BufferStorage.Stream, BufferAccess.None);

    public static BufferDescriptor MakeMappedReadOnly() =>
        new(BufferUsage.StaticDraw, BufferStorage.Static, BufferAccess.MapRead);

    public static BufferDescriptor MakeMappedWriteOnly() =>
        new(BufferUsage.DynamicDraw, BufferStorage.Dynamic, BufferAccess.MapWrite);

    public static BufferDescriptor MakePersistentMapped() =>
        new(BufferUsage.StreamDraw, BufferStorage.Stream,
            BufferAccess.MapRead | BufferAccess.MapWrite | BufferAccess.Persistent | BufferAccess.Coherent);
}