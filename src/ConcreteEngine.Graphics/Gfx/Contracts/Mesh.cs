using ConcreteEngine.Graphics.Gfx.Definitions;
using ConcreteEngine.Graphics.Gfx.Handles;

namespace ConcreteEngine.Graphics.Gfx.Contracts;

public sealed class MeshLayout(
    MeshId meshId,
    IndexBufferId iboId,
    VertexBufferId[] vboIds,
    VertexAttribute[] attributes)
{
    public readonly MeshId MeshId = meshId;
    public readonly IndexBufferId IboId = iboId;
    public readonly VertexBufferId[] VboIds = vboIds;
    public readonly VertexAttribute[] Attributes = attributes;
}

public readonly struct VertexLayout(
    byte slot,
    int components,
    int offset,
    VertexFormat format = VertexFormat.Float,
    bool normalized = false
)
{
    public readonly int Components = components;
    public readonly int Offset = offset;
    public readonly byte Slot = slot;
    public readonly VertexFormat Format = format;
    public readonly bool Normalized = normalized;
}

public readonly struct VertexAttribute(
    byte location,
    byte binding,
    int components,
    int offset,
    VertexFormat format = VertexFormat.Float,
    bool normalized = false
)
{
    public readonly ushort Components = (ushort)components;
    public readonly ushort Offset = (ushort)offset;
    public readonly byte Binding = binding;
    public readonly byte Location = location;
    public readonly VertexFormat Format = format;
    public readonly bool Normalized = normalized;
}

public readonly struct MeshDrawProperties(
    DrawPrimitive primitive,
    DrawMeshKind kind,
    DrawElementSize elementSize,
    int drawCount,
    int instanceCount = 0
)
{
    public DrawPrimitive Primitive { get; init; } = primitive;
    public DrawMeshKind Kind { get; init; } = kind;
    public DrawElementSize ElementSize { get; init; } = elementSize;
    public int DrawCount { get; init; } = drawCount;
    public int InstanceCount { get; init; } = instanceCount;

    public static MeshDrawProperties FromMeta(in MeshMeta meta) =>
        new(meta.Primitive, meta.Kind, meta.ElementSize, meta.DrawCount);

    public static MeshDrawProperties MakeArray(int drawCount = 0) =>
        new(DrawPrimitive.Triangles, DrawMeshKind.Arrays, DrawElementSize.Invalid, drawCount);

    public static MeshDrawProperties MakeInstance(DrawPrimitive primitive, int drawCount, int instances) =>
        new(primitive, DrawMeshKind.ArraysInstanced, DrawElementSize.Invalid, drawCount, instanceCount: instances);


    public static MeshDrawProperties MakeElemental(DrawMeshKind kind = DrawMeshKind.Elements,
        DrawElementSize size = DrawElementSize.UnsignedInt, int drawCount = 0) =>
        new(DrawPrimitive.Triangles, kind, size, drawCount);
}