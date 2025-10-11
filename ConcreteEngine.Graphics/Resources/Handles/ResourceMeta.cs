#region

using System.Runtime.InteropServices;
using ConcreteEngine.Common.Numerics;
using ConcreteEngine.Graphics.Gfx.Contracts;

#endregion

namespace ConcreteEngine.Graphics.Resources;

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
    GfxPixelFormat pixelFormat,
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
    public readonly GfxPixelFormat PixelFormat = pixelFormat;
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
public readonly struct ShaderMeta(int samplers) : IResourceMeta
{
    public readonly int Samplers = samplers;
}

[StructLayout(LayoutKind.Sequential)]
public readonly struct MeshMeta(
    DrawPrimitive primitive,
    MeshDrawKind drawKind,
    DrawElementSize elementSize,
    int attributeLength,
    int drawCount
) : IResourceMeta
{
    public readonly int AttributeLength = attributeLength;
    public readonly int DrawCount = drawCount;
    public readonly DrawPrimitive Primitive = primitive;
    public readonly MeshDrawKind DrawKind = drawKind;
    public readonly DrawElementSize ElementSize = elementSize;


    public static MeshMeta CreateCopy(in MeshMeta meta, int vertexAttribPointers, int drawCount) =>
        new(meta.Primitive, meta.DrawKind, meta.ElementSize, vertexAttribPointers, drawCount);
}

[StructLayout(LayoutKind.Sequential)]
public readonly struct VertexBufferMeta(
    int bindingIdx,
    int elementCount,
    nint stride,
    BufferUsage usage,
    BufferStorage storage,
    BufferAccess access
) : IResourceMeta
{
    public readonly nint Stride = stride;
    public readonly int BindingIdx = bindingIdx;
    public readonly int ElementCount = elementCount;
    public readonly BufferUsage Usage = usage;
    public readonly BufferStorage Storage = storage;
    public readonly BufferAccess Access = access;

    public static VertexBufferMeta CreateCopy(in VertexBufferMeta meta, int count, nint stride, BufferUsage usage) =>
        new(meta.BindingIdx, count, stride, usage, meta.Storage, meta.Access);
}

[StructLayout(LayoutKind.Sequential)]
public readonly struct IndexBufferMeta(
    int elementCount,
    nint stride,
    BufferUsage usage,
    BufferStorage storage,
    BufferAccess access
) : IResourceMeta
{
    public readonly nint Stride = stride;
    public readonly int ElementCount = elementCount;
    public readonly BufferUsage Usage = usage;
    public readonly BufferStorage Storage = storage;
    public readonly BufferAccess Access = access;

    public static IndexBufferMeta CreateCopy(in IndexBufferMeta meta, int count, nint stride, BufferUsage usage) =>
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
    FrameBufferAttachmentKind attachmentKind,
    RenderBufferMsaa multisample
) : IResourceMeta
{
    public readonly Size2D Size = size;
    public readonly FrameBufferAttachmentKind AttachmentKind = attachmentKind;
    public readonly RenderBufferMsaa Multisample = multisample;
}

[StructLayout(LayoutKind.Sequential)]
public readonly struct UniformBufferMeta(
    UboSlot slot,
    nint stride,
    nint capacity,
    BufferUsage usage,
    BufferStorage storage,
    BufferAccess access)
    : IResourceMeta
{
    public readonly nint Capacity = capacity;
    public readonly nint Stride = stride;
    public readonly UboSlot Slot = slot;
    public readonly BufferUsage Usage = usage;
    public readonly BufferStorage Storage = storage;
    public readonly BufferAccess Access = access;

    public static UniformBufferMeta MakeResizeCopy(in UniformBufferMeta meta, nint capacity) =>
        new(meta.Slot, meta.Stride, capacity, meta.Usage, meta.Storage, meta.Access);
}