using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using ConcreteEngine.Core.Common.Numerics;

namespace ConcreteEngine.Graphics.Gfx;

public interface IResourceMeta
{
    static abstract GraphicsKind ResourceKind { get; }
}

public struct SamplerMeta(
    SamplerProfile profile,
    float lod,
    TexturePreset preset,
    TextureAnisotropy anisotropy = TextureAnisotropy.Off,
    TextureBorder border = TextureBorder.Off,
    DepthMode depthMode = DepthMode.Unset)
    : IResourceMeta
{
    public Half Lod = (Half)lod;
    public SamplerProfile Profile = profile;
    public TexturePreset Preset = preset;
    public TextureAnisotropy Anisotropy = anisotropy;
    public TextureBorder Border = border;
    public DepthMode DepthMode = depthMode;

    public static GraphicsKind ResourceKind => GraphicsKind.Texture;
}

[StructLayout(LayoutKind.Sequential)]
public readonly struct TextureMeta(
    int width,
    int height,
    int depth,
    int mipLevels,
    int samples,
    TextureKind kind,
    TexturePixelFormat pixelFormat,
    TextureBorder borderColor) : IResourceMeta
{
    public int Width { get; init; } = width;
    public int Height { get; init; } = height;
    public ushort Depth { get; init; } = (ushort)depth;
    public TextureBorder BorderColor { get; init; } = borderColor;
    public byte MipLevels { get; init; } = (byte)mipLevels;
    public byte Samples { get; init; } = (byte)samples;
    public TextureKind Kind { get; init; } = kind;
    public TexturePixelFormat PixelFormat { get; init; } = pixelFormat;

    public bool IsMipMapped => MipLevels > 1;
    public bool IsMsaa => Kind == TextureKind.Multisample2D && Samples > 0;

    public int GetArrayLength() => Kind == TextureKind.Texture2DArray ? Depth : 0;
    public Size2D AsSize2D() => new(Width, Height);
    public Size3D AsSize3D() => new(Width, Height, Depth);

    public static GraphicsKind ResourceKind => GraphicsKind.Texture;
}

[StructLayout(LayoutKind.Sequential)]
public readonly struct ShaderMeta(int samplerSlots) : IResourceMeta
{
    public readonly int SamplerSlots = samplerSlots;
    public static GraphicsKind ResourceKind => GraphicsKind.Shader;
}

[StructLayout(LayoutKind.Sequential)]
public readonly struct MeshMeta : IResourceMeta
{
    public uint DrawCount { get; init; }
    public uint InstanceCount { get; init; }
    public DrawPrimitive Primitive { get; init; }
    public DrawMeshKind Kind { get; init; }
    public DrawElementSize ElementSize { get; init; }
    public static GraphicsKind ResourceKind => GraphicsKind.Mesh;
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
    public int Stride { get; init; } = stride;
    public int ElementCount { get; init; } = elementCount;
    public uint Offset { get; init; } = offset;
    public byte Divisor { get; init; } = divisor;
    public BufferUsage Usage { get; init; } = usage;
    public BufferStorage Storage { get; init; } = storage;
    public BufferAccess Access { get; init; } = access;

    public long Capacity => Stride * ElementCount;

    public static GraphicsKind ResourceKind => GraphicsKind.VertexBuffer;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool HasCapacity(int elementCount) => elementCount * Stride <= Capacity;

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
    public static GraphicsKind ResourceKind => GraphicsKind.IndexBuffer;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool HasCapacity(int elementCount) => elementCount * Stride <= Capacity;

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
    public static GraphicsKind ResourceKind => GraphicsKind.FrameBuffer;

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
    public static GraphicsKind ResourceKind => GraphicsKind.RenderBuffer;
}

[StructLayout(LayoutKind.Sequential)]
public readonly struct UniformBufferMeta(
    byte slot,
    int stride,
    int capacity,
    BufferUsage usage,
    BufferStorage storage,
    BufferAccess access)
    : IResourceMeta
{
    public readonly int Capacity = capacity;
    public readonly int Stride = stride;
    public readonly byte Slot = slot;
    public readonly BufferUsage Usage = usage;
    public readonly BufferStorage Storage = storage;
    public readonly BufferAccess Access = access;
    public static GraphicsKind ResourceKind => GraphicsKind.UniformBuffer;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool HasCapacity(int elementCount) => elementCount * Stride <= Capacity;

    public static UniformBufferMeta MakeResizeCopy(in UniformBufferMeta meta, int capacity) =>
        new(meta.Slot, meta.Stride, capacity, meta.Usage, meta.Storage, meta.Access);
}