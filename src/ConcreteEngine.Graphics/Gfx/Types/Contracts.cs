using ConcreteEngine.Core.Common.Numerics;

namespace ConcreteEngine.Graphics.Gfx;

internal readonly struct GpuTextureProps(TexturePixelFormat format, uint levels, uint samples)
{
    public readonly uint Levels = levels, Samples = samples;
    public readonly TexturePixelFormat Format = format;

    public static GpuTextureProps Make(TexturePixelFormat format, int levels, int samples) =>
        new(format, (uint)levels, (uint)samples);
}

public struct CreateFboInfo(Size2D size)
{
    public Size2D Size = size;
    public FboColorAttachment ColorTexture;
    public FboDepthAttachment DepthTexture;
    public bool ColorBuffer;
    public bool DepthStencilBuffer;
    public RenderBufferMsaa Multisample = RenderBufferMsaa.None;

    public readonly CreateFboInfo AttachColorTexture(FboColorAttachment attachment, RenderBufferMsaa multisample = 0)
    {
        return this with { ColorTexture = attachment, Multisample = multisample };
    }

    public readonly CreateFboInfo AttachDepthTexture(FboDepthAttachment attachment)
    {
        return this with { DepthTexture = attachment };
    }

    public CreateFboInfo AttachDepthStencilBuffer()
    {
        return this with { DepthStencilBuffer = true };
    }
}

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
    public int Offset { get; init; } = offset;
    public int Length { get; init; } = length;
    public BufferStorage Storage { get; init; } = storage;
    public BufferAccess Access { get; init; } = access;
    public byte Binding { get; init; } = binding;
    public byte Divisor { get; init; } = divisor;

    public static CreateVboArgs MakeDefault(int binding) => new(binding: (byte)binding);

    public static CreateVboArgs MakeInstance(int binding, int divisor, int length) =>
        new(storage: BufferStorage.Dynamic, BufferAccess.MapWrite, divisor: (byte)divisor, binding: (byte)binding,
            length: length);

    public static CreateVboArgs MakeDynamic(int binding) =>
        new(storage: BufferStorage.Dynamic, BufferAccess.MapWrite, binding: (byte)binding);
}

public readonly struct CreateIboArgs(
    BufferStorage storage = BufferStorage.Static,
    BufferAccess access = BufferAccess.None,
    int length = 0)
{
    public int Length { get; init; } = length;
    public BufferStorage Storage { get; init; } = storage;
    public BufferAccess Access { get; init; } = access;

    public static CreateIboArgs MakeDefault() => new(BufferStorage.Static, BufferAccess.None, 0);
}