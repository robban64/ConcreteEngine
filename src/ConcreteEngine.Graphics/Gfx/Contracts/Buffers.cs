using ConcreteEngine.Graphics.Gfx.Definitions;

namespace ConcreteEngine.Graphics.Gfx.Contracts;

internal readonly struct CreateBufferInfo(uint size, BufferStorage storage, BufferAccess access)
{
    public readonly uint Size = size;
    public readonly BufferStorage Storage = storage;
    public readonly BufferAccess Access = access;
}

public readonly struct CreateVboArgs(
    BufferStorage storage = BufferStorage.Static,
    BufferAccess access = BufferAccess.None,
    byte binding = 0,
    byte divisor = 0,
    int offset = 0,
    int length = 0)
{
    public int Offset { get; } = offset;
    public int Length { get; } = length;
    public BufferStorage Storage { get; } = storage;
    public BufferAccess Access { get; } = access;
    public byte Binding { get; } = binding;
    public byte Divisor { get; } = divisor;

    public static CreateVboArgs MakeDefault(int binding) => new(binding: (byte)binding);

    public static CreateVboArgs MakeInstance(int binding, int divisor, int length) => new(
        storage: BufferStorage.Dynamic, BufferAccess.MapWrite, divisor: (byte)divisor, binding: (byte)binding,
        length: length);

    public static CreateVboArgs MakeDynamic(int binding) =>
        new(storage: BufferStorage.Dynamic, BufferAccess.MapWrite, binding: (byte)binding);
}

public readonly struct CreateIboArgs(
    BufferStorage storage = BufferStorage.Static,
    BufferAccess access = BufferAccess.None,
    int length = 0)
{
    public int Length { get; } = length;
    public BufferStorage Storage { get; } = storage;
    public BufferAccess Access { get; } = access;

    public static CreateIboArgs MakeDefault() => new(BufferStorage.Static, BufferAccess.None, 0);
}

/*
public struct BufferDescriptor(
    BufferUsage usage,
    BufferStorage storage,
    BufferAccess access)
{
    public BufferUsage Usage = usage;
    public BufferStorage Storage = storage;
    public BufferAccess Access = access;


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
}*/