#region

using System.Numerics;
using ConcreteEngine.Graphics.Descriptors;
using Silk.NET.Maths;

#endregion

namespace ConcreteEngine.Graphics.Resources;

public enum ResourceKind : ushort
{
    Texture = 0,
    Shader = 1,
    Mesh = 2,
    VertexBuffer = 3,
    IndexBuffer = 4,
    FrameBuffer = 5,
    RenderBuffer = 6
}

public readonly struct TextureMeta(Vector2D<int> size, EnginePixelFormat format)
{
    public readonly Vector2D<int> Size = size;
    public readonly EnginePixelFormat Format = format;
}

public readonly struct ShaderMeta(uint samplers)
{
    public readonly uint Samplers = samplers;
}

public readonly struct MeshMeta(
    VertexBufferId vertexBufferId,
    IndexBufferId indexBufferId,
    DrawPrimitive primitive,
    IboElementType elementType,
    bool isStatic,
    uint drawCount,
    uint vertexLayoutCount,
    VertexAttributeDescriptor vertexLayout1,
    VertexAttributeDescriptor vertexLayout2 = default,
    VertexAttributeDescriptor vertexLayout3 = default
)
{
    public readonly VertexBufferId VertexBufferId = vertexBufferId;
    public readonly IndexBufferId IndexBufferId = indexBufferId;
    public readonly DrawPrimitive Primitive = primitive;
    public readonly IboElementType ElementType = elementType;
    public readonly bool IsStatic = isStatic;
    public readonly uint DrawCount = drawCount;
    public readonly uint VertexLayoutCount = vertexLayoutCount;
    public readonly VertexAttributeDescriptor VertexLayout1 = vertexLayout1;
    public readonly VertexAttributeDescriptor VertexLayout2 = vertexLayout2;
    public readonly VertexAttributeDescriptor VertexLayout3 = vertexLayout2;
}

public readonly struct VertexBufferMeta(
    BufferUsage usage,
    uint elementCount,
    uint elementSize
)
{
    public readonly BufferUsage Usage = usage;
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

    public static void GetResizeCopy(in FrameBufferMeta m, Vector2D<int> size, out FrameBufferMeta meta)
    {
        meta = new FrameBufferMeta(m.ColTexId, m.RboTexId, m.RboDepthId, m.TexturePreset, m.SizeRatio, size,
            m.DepthStencilBuffer,
            m.Msaa, m.Samples);
    }
}

//TODO
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