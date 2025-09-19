#region

using System.Numerics;
using ConcreteEngine.Graphics.Descriptors;
using ConcreteEngine.Graphics.Utils;
using Silk.NET.Maths;

#endregion

namespace ConcreteEngine.Graphics.Resources;

public interface IResourceMeta;

public readonly struct TextureMeta(
    uint width,
    uint height,
    TexturePreset preset,
    TextureKind kind,
    TextureAnisotropy anisotropy,
    EnginePixelFormat format,
    byte mipLevel,
    bool hasData) : IResourceMeta
{
    public readonly uint Width = width;
    public readonly uint Height = height;
    public readonly TexturePreset Preset = preset;
    public readonly TextureKind Kind = kind;
    public readonly TextureAnisotropy Anisotropy = anisotropy;
    public readonly EnginePixelFormat PixelFormat = format;
    public readonly byte MipLevels = mipLevel;
    public readonly bool HasData = hasData;

    internal static TextureMeta CreateFromHasData(in TextureMeta m, bool hasData) =>
        new(m.Width, m.Height, m.Preset, m.Kind, m.Anisotropy, m.PixelFormat, m.MipLevels, hasData);
}

public readonly struct ShaderMeta(uint samplers) : IResourceMeta
{
    public readonly uint Samplers = samplers;
}

public readonly struct MeshMeta(
    DrawPrimitive primitive,
    MeshDrawKind drawKind,
    DrawElementSize elementSize,
    uint vertexAttribPointers,
    uint drawCount
) : IResourceMeta
{
    public readonly uint VertexAttribPointers = vertexAttribPointers;
    public readonly uint DrawCount = drawCount;
    public readonly DrawPrimitive Primitive = primitive;
    public readonly MeshDrawKind DrawKind = drawKind;
    public readonly DrawElementSize ElementSize = elementSize;


    public static MeshMeta CreateCopy(in MeshMeta meta, uint vertexAttribPointers, uint drawCount) =>
        new(meta.Primitive, meta.DrawKind, meta.ElementSize, vertexAttribPointers, drawCount);
}

public readonly struct VertexBufferMeta(
    uint bindingIdx,
    uint elementCount,
    uint elementSize,
    BufferUsage usage,
    BufferStorage storage,
    BufferAccess access
) : IResourceMeta
{
    public readonly uint BindingIdx = bindingIdx;
    public readonly uint ElementCount = elementCount;
    public readonly uint ElementSize = elementSize;
    public readonly BufferUsage Usage = usage;
    public readonly BufferStorage Storage = storage;
    public readonly BufferAccess Access = access;
    
    public static VertexBufferMeta CreateCopy(in VertexBufferMeta meta, uint count, uint size, BufferUsage usage)
        => new(meta.BindingIdx, count, size, usage, meta.Storage, meta.Access);
}

public readonly struct IndexBufferMeta(
    uint elementCount,
    uint elementSize,
    BufferUsage usage,
    BufferStorage storage,
    BufferAccess access
) : IResourceMeta
{
    public readonly uint ElementCount = elementCount;
    public readonly uint ElementSize = elementSize;
    public readonly BufferUsage Usage = usage;
    public readonly BufferStorage Storage = storage;
    public readonly BufferAccess Access = access;

    public static IndexBufferMeta CreateCopy(in IndexBufferMeta meta, uint count, uint size, BufferUsage usage)
        => new(count, size, usage, meta.Storage, meta.Access);
}

public readonly struct FrameBufferMeta(
    Vector2D<int> size,
    bool colorBuffer,
    bool depthBuffer,
    bool depthStencilBuffer
) : IResourceMeta
{
    public readonly Vector2D<int> Size = size;
    public readonly bool ColorBuffer = colorBuffer;
    public readonly bool DepthBuffer = depthBuffer;
    public readonly bool DepthStencilBuffer = depthStencilBuffer;

    internal static FrameBufferMeta CreateResizeCopy(in FrameBufferMeta meta, Vector2D<int> size) =>
        new(size, meta.ColorBuffer, meta.DepthBuffer, meta.DepthStencilBuffer);
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
    public readonly nuint BlockSize;
    public readonly nuint Stride;
    public readonly uint BindingIdx;
    public readonly UniformGpuSlot Slot;
    public readonly BufferUsage Usage;
    public readonly BufferStorage Storage;
    public readonly BufferAccess Access;

    public UniformBufferMeta(UniformGpuSlot slot, nuint blockSize, BufferUsage usage, BufferStorage storage,
        BufferAccess access)
    {
        Slot = slot;
        BindingIdx = (uint)slot;
        BlockSize = blockSize;
        Usage = usage;
        Storage = storage;
        Access = access;
        Stride = UniformBufferUtils.AlignUp(BlockSize, UniformBufferUtils.UboOffsetAlign);
    }
}