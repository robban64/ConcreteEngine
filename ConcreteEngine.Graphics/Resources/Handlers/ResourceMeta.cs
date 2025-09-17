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
        new (m.Width, m.Height, m.Preset,m.Kind,m.Anisotropy,m.PixelFormat,m.MipLevels, hasData);
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
    BufferUsage usage,
    uint bindingIdx,
    uint elementCount,
    uint elementSize
) : IResourceMeta
{
    public readonly uint BindingIdx = bindingIdx;
    public readonly uint ElementCount = elementCount;
    public readonly uint ElementSize = elementSize;
    public readonly BufferUsage Usage = usage;
}

public readonly struct IndexBufferMeta(
    BufferUsage usage,
    uint elementCount,
    uint elementSize
) : IResourceMeta
{
    public readonly uint ElementCount = elementCount;
    public readonly uint ElementSize = elementSize;
    public readonly BufferUsage Usage = usage;
}

public readonly struct FrameBufferMeta(
    Vector2D<int> size,
    bool depthStencilBuffer,
    bool msaa,
    byte samples
) : IResourceMeta
{
    public readonly Vector2D<int> Size = size;
    public readonly byte Samples = samples;
    public readonly bool DepthStencilBuffer = depthStencilBuffer;
    public readonly bool Msaa = msaa;

    internal static FrameBufferMeta CreateResizeCopy(in FrameBufferMeta meta, Vector2D<int> size) =>
        new(size, meta.DepthStencilBuffer, meta.Msaa, meta.Samples);
}

public readonly struct RenderBufferMeta(
    RenderBufferKind kind,
    Vector2D<int> size,
    bool multisample
) : IResourceMeta
{
    public readonly Vector2D<int> Size = size;
    public readonly RenderBufferKind Kind = kind;
    public readonly bool Multisample = multisample;
}

public readonly struct UniformBufferMeta : IResourceMeta
{
    public readonly nuint BlockSize;
    public readonly nuint Stride;
    public readonly uint BindingIdx;
    public readonly UniformGpuSlot Slot;

    public UniformBufferMeta(UniformGpuSlot slot, nuint blockSize)
    {
        Slot = slot;
        BindingIdx = (uint)slot;
        BlockSize = blockSize;
        Stride = UniformBufferUtils.AlignUp(BlockSize, UniformBufferUtils.UboOffsetAlign);
    }
}