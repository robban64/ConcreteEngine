#region

using System.Numerics;
using ConcreteEngine.Graphics.Descriptors;
using ConcreteEngine.Graphics.Utils;
using Silk.NET.Maths;

#endregion

namespace ConcreteEngine.Graphics.Resources;

public enum ResourceKind : byte
{
    Texture = 0,
    Shader = 1,
    Mesh = 2,
    VertexBuffer = 3,
    IndexBuffer = 4,
    FrameBuffer = 5,
    RenderBuffer = 6
}

public readonly struct TextureMeta(int width, int height, EnginePixelFormat format)
{
    public readonly int Width = width;
    public readonly int Height = height;
    public readonly EnginePixelFormat Format = format;
}

public readonly struct ShaderMeta(uint samplers)
{
    public readonly uint Samplers = samplers;
}

public readonly struct MeshMeta(
    DrawPrimitive primitive,
    MeshDrawKind drawKind,
    DrawElementType elementType,
    uint vertexAttribPointers,
    uint drawCount
)
{
    public readonly DrawPrimitive Primitive = primitive;
    public readonly MeshDrawKind DrawKind = drawKind;
    public readonly DrawElementType ElementType = elementType;
    public readonly uint VertexAttribPointers = vertexAttribPointers;
    public readonly uint DrawCount = drawCount;
}

public readonly struct VertexBufferMeta(
    BufferUsage usage,
    uint bindingIdx,
    uint elementCount,
    uint elementSize
)
{
    public readonly BufferUsage Usage = usage;
    public readonly uint BindingIdx = bindingIdx;
    public readonly uint ElementCount = elementCount;
    public readonly uint ElementSize = elementSize;
}

public readonly struct IndexBufferMeta(
    BufferUsage usage,
    uint elementCount,
    uint elementSize
)
{
    public readonly BufferUsage Usage = usage;
    public readonly uint ElementCount = elementCount;
    public readonly uint ElementSize = elementSize;
}

public readonly struct FrameBufferMeta(
    TextureId colTexId,
    RenderBufferId rboTexId,
    RenderBufferId rboDepthId,
    TexturePreset texturePreset,
    Vector2 sizeRatio,
    Vector2D<int> size,
    bool depthStencilBuffer,
    bool msaa,
    byte samples
)
{
    public readonly TextureId ColTexId = colTexId;
    public readonly RenderBufferId RboTexId = rboTexId;
    public readonly RenderBufferId RboDepthId = rboDepthId;
    public readonly TexturePreset TexturePreset = texturePreset;
    public readonly Vector2 SizeRatio = sizeRatio;
    public readonly Vector2D<int> Size = size;
    public readonly bool DepthStencilBuffer = depthStencilBuffer;
    public readonly bool Msaa = msaa;
    public readonly byte Samples = samples;
}

public readonly struct RenderBufferMeta(
    RenderBufferKind kind,
    Vector2D<int> size,
    bool multisample
)
{
    public readonly RenderBufferKind Kind = kind;
    public readonly Vector2D<int> Size = size;
    public readonly bool Multisample = multisample;
}

public readonly struct UniformBufferMeta
{
    public readonly UniformGpuSlot Slot;
    public readonly uint BindingIdx;
    public readonly nuint BlockSize;
    public readonly nuint Stride;

    public UniformBufferMeta(UniformGpuSlot slot, nuint blockSize)
    {
        Slot = slot;
        BindingIdx = (uint)slot;
        BlockSize = blockSize;
        Stride = UniformBufferUtils.AlignUp(BlockSize, UniformBufferUtils.UboOffsetAlign);
    }
}