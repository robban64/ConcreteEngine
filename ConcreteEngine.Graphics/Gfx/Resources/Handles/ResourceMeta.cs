using System.Runtime.InteropServices;
using ConcreteEngine.Common.Numerics;
using ConcreteEngine.Graphics.Gfx.Contracts;
using ConcreteEngine.Graphics.Gfx.Definitions;

namespace ConcreteEngine.Graphics.Gfx.Resources;

public interface IResourceMeta;

[StructLayout(LayoutKind.Sequential)]
public readonly struct TextureMeta(
    Half lod,
    ushort width,
    ushort height,
    ushort depth,
    byte levels,
    byte samples,
    TexturePreset preset,
    TextureKind kind,
    TextureAnisotropy anisotropy,
    TexturePixelFormat pixelFormat,
    DepthMode compareTextureFunc,
    GfxTextureBorder borderColor) : IResourceMeta
{
    public readonly Half Lod = lod;
    public readonly ushort Width = width;
    public readonly ushort Height = height;
    public readonly ushort Depth = depth;
    public readonly byte Levels = levels;
    public readonly byte Samples = samples;
    public readonly TexturePreset Preset = preset;
    public readonly TextureKind Kind = kind;
    public readonly TextureAnisotropy Anisotropy = anisotropy;
    public readonly TexturePixelFormat PixelFormat = pixelFormat;
    public readonly DepthMode CompareTextureFunc = compareTextureFunc;
    public readonly GfxTextureBorder BorderColor = borderColor;

    public bool IsMipMapped => Levels > 1;
    public bool IsMsaa => Kind == TextureKind.Multisample2D && Samples > 0;

    internal static TextureMeta CopyWithNewSize(in TextureMeta m) =>
        new(width: m.Width, height: m.Height, depth: m.Depth,
            levels: m.Levels, samples: m.Samples,
            preset: m.Preset, kind: m.Kind, anisotropy: m.Anisotropy,
            pixelFormat: m.PixelFormat, lod: m.Lod,
            compareTextureFunc: m.CompareTextureFunc, borderColor: m.BorderColor
        );
}

[StructLayout(LayoutKind.Sequential)]
public readonly struct ShaderMeta(int samplerSlots) : IResourceMeta
{
    public readonly int SamplerSlots = samplerSlots;
}

[StructLayout(LayoutKind.Sequential)]
public readonly struct MeshMeta : IResourceMeta
{
    public int DrawCount { get; init; }
    public int InstanceCount { get; init; }
    public int AttributeCount { get; init; }
    public byte VboCount { get; init; }
    public DrawPrimitive Primitive { get; init; }
    public DrawMeshKind Kind { get; init; }
    public DrawElementSize ElementSize { get; init; }
}

[StructLayout(LayoutKind.Sequential)]
public readonly struct VertexBufferMeta(
    int stride,
    int elementCount,
    uint offset,
    byte divisor,
    BufferUsage usage,
    BufferStorage storage,
    BufferAccess access
) : IResourceMeta
{
    public readonly int Stride = stride;
    public readonly int ElementCount = elementCount;
    public readonly uint Offset = offset;
    public readonly byte Divisor = divisor;
    public readonly BufferUsage Usage = usage;
    public readonly BufferStorage Storage = storage;
    public readonly BufferAccess Access = access;

    public nint Capacity => Stride * ElementCount;

    public static VertexBufferMeta CreateCopy(in VertexBufferMeta m, int count, int stride, uint offset,
        BufferUsage usage) =>
        new(stride, count, offset, m.Divisor, usage, m.Storage, m.Access);
}

[StructLayout(LayoutKind.Sequential)]
public readonly struct IndexBufferMeta(
    int elementCount,
    int stride,
    BufferUsage usage,
    BufferStorage storage,
    BufferAccess access
) : IResourceMeta
{
    public readonly int Stride = stride;
    public readonly int ElementCount = elementCount;
    public readonly BufferUsage Usage = usage;
    public readonly BufferStorage Storage = storage;
    public readonly BufferAccess Access = access;

    public nint Capacity => Stride * ElementCount;

    public static IndexBufferMeta CreateCopy(in IndexBufferMeta meta, int count, int stride, BufferUsage usage) =>
        new(count, stride, usage, meta.Storage, meta.Access);
}

[StructLayout(LayoutKind.Sequential)]
public readonly struct FrameBufferMeta(
    Size2D size,
    FboAttachmentIds attachments,
    RenderBufferMsaa multiSample
) : IResourceMeta
{
    public readonly Size2D Size = size;
    public readonly FboAttachmentIds Attachments = attachments;
    public readonly RenderBufferMsaa MultiSample = multiSample;

    internal static FrameBufferMeta MakeResizeCopy(in FrameBufferMeta meta, Size2D size) =>
        new(size, meta.Attachments, meta.MultiSample);
}

[StructLayout(LayoutKind.Sequential)]
public readonly struct RenderBufferMeta(
    Size2D size,
    FrameBufferAttachmentSlot attachmentSlot,
    RenderBufferMsaa multisample
) : IResourceMeta
{
    public readonly Size2D Size = size;
    public readonly FrameBufferAttachmentSlot AttachmentSlot = attachmentSlot;
    public readonly RenderBufferMsaa Multisample = multisample;
}

[StructLayout(LayoutKind.Sequential)]
public readonly struct UniformBufferMeta(
    UboSlot slot,
    int stride,
    nint capacity,
    BufferUsage usage,
    BufferStorage storage,
    BufferAccess access)
    : IResourceMeta
{
    public readonly nint Capacity = capacity;
    public readonly int Stride = stride;
    public readonly UboSlot Slot = slot;
    public readonly BufferUsage Usage = usage;
    public readonly BufferStorage Storage = storage;
    public readonly BufferAccess Access = access;

    public static UniformBufferMeta MakeResizeCopy(in UniformBufferMeta meta, nint capacity) =>
        new(meta.Slot, meta.Stride, capacity, meta.Usage, meta.Storage, meta.Access);
}