#region

using ConcreteEngine.Common.Numerics;
using ConcreteEngine.Graphics.Gfx.Utility;
using Silk.NET.Maths;

#endregion

namespace ConcreteEngine.Graphics.Resources;

public interface IResourceMeta;

public readonly struct TextureMeta(
    int width,
    int height,
    TexturePreset preset,
    TextureKind kind,
    TextureAnisotropy anisotropy,
    EnginePixelFormat format,
    float lod,
    int depth,
    short levels,
    short samples,
    nint sizeInBytes
) : IResourceMeta
{
    public readonly nint SizeInBytes = sizeInBytes;
    public readonly int Width = width;
    public readonly int Height = height;
    public readonly int Depth = depth;
    public readonly float Lod = lod;
    public readonly short Levels = levels;
    public readonly short Samples = samples;
    public readonly TexturePreset Preset = preset;
    public readonly TextureKind Kind = kind;
    public readonly TextureAnisotropy Anisotropy = anisotropy;
    public readonly EnginePixelFormat PixelFormat = format;

    public bool IsMipMapped => Levels > 1;
    public bool IsMsaa => Kind == TextureKind.Multisample2D && Samples > 0;

    internal static TextureMeta CopyWithNewSize(in TextureMeta m, nint sizeInBytes) =>
        new(width: m.Width, height: m.Height, preset: m.Preset, kind: m.Kind, anisotropy: m.Anisotropy,
            format: m.PixelFormat,
            lod: m.Lod, depth: m.Depth, levels: m.Levels, samples: m.Samples, sizeInBytes: sizeInBytes
        );
}

public readonly struct ShaderMeta(int samplers) : IResourceMeta
{
    public readonly int Samplers = samplers;
}

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

public readonly struct RenderBufferMeta(
    Size2D size,
    FrameBufferTarget target,
    RenderBufferMsaa multisample
) : IResourceMeta
{
    public readonly Size2D Size = size;
    public readonly FrameBufferTarget Target = target;
    public readonly RenderBufferMsaa Multisample = multisample;
}

public readonly struct UniformBufferMeta : IResourceMeta
{
    public readonly nint BlockSize;
    public readonly nint Stride;
    public readonly UniformGpuSlot Slot;
    public readonly BufferUsage Usage;
    public readonly BufferStorage Storage;
    public readonly BufferAccess Access;


    public UniformBufferMeta(UniformGpuSlot slot, nint blockSize, BufferUsage usage, BufferStorage storage,
        BufferAccess access)
    {
        Slot = slot;
        BlockSize = blockSize;
        Usage = usage;
        Storage = storage;
        Access = access;
        Stride = UniformBufferUtils.AlignUp(BlockSize, UniformBufferUtils.UboOffsetAlign);
    }
}