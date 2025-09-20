#region

using ConcreteEngine.Graphics.Utils;
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
    byte mipLevel,
    bool hasData) : IResourceMeta
{
    public readonly int Width = width;
    public readonly int Height = height;
    public readonly TexturePreset Preset = preset;
    public readonly TextureKind Kind = kind;
    public readonly TextureAnisotropy Anisotropy = anisotropy;
    public readonly EnginePixelFormat PixelFormat = format;
    public readonly byte MipLevels = mipLevel;
    public readonly bool HasData = hasData;

    internal static TextureMeta CreateFromHasData(in TextureMeta m, bool hasData) =>
        new(m.Width, m.Height, m.Preset, m.Kind, m.Anisotropy, m.PixelFormat, m.MipLevels, hasData);
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
    public readonly int BindingIdx = bindingIdx;
    public readonly int ElementCount = elementCount;
    public readonly nint Stride = stride;
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
    public readonly int ElementCount = elementCount;
    public readonly nint Stride = stride;
    public readonly BufferUsage Usage = usage;
    public readonly BufferStorage Storage = storage;
    public readonly BufferAccess Access = access;

    public static IndexBufferMeta CreateCopy(in IndexBufferMeta meta, int count, nint stride, BufferUsage usage) =>
        new(count, stride, usage, meta.Storage, meta.Access);
}

public readonly struct FrameBufferMeta(
    Vector2D<int> size,
    bool colorTexture,
    bool depthTexture,
    bool colorBuffer,
    bool depthStencilBuffer
) : IResourceMeta
{
    public readonly Vector2D<int> Size = size;
    public readonly bool ColorTexture = colorTexture;
    public readonly bool DepthTexture = depthTexture;
    public readonly bool ColorBuffer = colorBuffer;
    public readonly bool DepthStencilBuffer = depthStencilBuffer;

    internal static FrameBufferMeta MakeResizeCopy(in FrameBufferMeta meta, Vector2D<int> size) =>
        new(size, meta.ColorTexture, meta.DepthTexture, meta.ColorBuffer, meta.DepthStencilBuffer);
}

public readonly struct RenderBufferMeta(
    Vector2D<int> size,
    FrameBufferTarget target,
    RenderBufferMsaa multisample
) : IResourceMeta
{
    public readonly Vector2D<int> Size = size;
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