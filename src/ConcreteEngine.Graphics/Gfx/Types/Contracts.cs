using ConcreteEngine.Core.Common.Numerics;
using ConcreteEngine.Graphics.Gfx.Definitions;

namespace ConcreteEngine.Graphics.Gfx.Types;

internal readonly struct GpuTextureProps(TexturePixelFormat format, uint levels, uint samples)
{
    public readonly uint Levels = levels, Samples = samples;
    public readonly TexturePixelFormat Format = format;

    public static GpuTextureProps Make(TexturePixelFormat format, int levels, int samples) =>
        new(format, (uint)levels, (uint)samples);
}


public readonly struct CreateTextureProps(
    float lod,
    TextureKind kind,
    TexturePixelFormat format,
    TexturePreset preset,
    TextureAnisotropy anisotropy,
    DepthMode compareTextureFunc = DepthMode.Unset,
    GpuTextureBorder borderColor = default,
    RenderBufferMsaa samples = RenderBufferMsaa.None
)
{
    public readonly GpuTextureBorder BorderColor = borderColor;
    
    public readonly Half Lod = (Half)lod;
    public readonly TextureKind Kind = kind;
    public readonly TexturePixelFormat Format = format;

    public readonly TexturePreset Preset = preset;
    public readonly TextureAnisotropy Anisotropy = anisotropy;
    public readonly DepthMode CompareTextureFunc = compareTextureFunc;
    public readonly RenderBufferMsaa Samples = samples;

}


public readonly struct CreateFboInfo(
    Size2D size,
    FboColorAttachment? colorTexture,
    FboDepthAttachment? depthTexture,
    bool colorBuffer,
    bool depthStencilBuffer,
    RenderBufferMsaa multisample = RenderBufferMsaa.None
)
{
    public readonly Size2D Size = size;
    public readonly FboColorAttachment? ColorTexture = colorTexture;
    public readonly FboDepthAttachment? DepthTexture = depthTexture;
    public readonly bool ColorBuffer = colorBuffer;
    public readonly bool DepthStencilBuffer = depthStencilBuffer;
    public readonly RenderBufferMsaa Multisample = multisample;
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
