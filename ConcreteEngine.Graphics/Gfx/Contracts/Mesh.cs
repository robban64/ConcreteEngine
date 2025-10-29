#region

using ConcreteEngine.Graphics.Gfx.Definitions;

#endregion

namespace ConcreteEngine.Graphics.Gfx.Contracts;

public readonly record struct VboAttachment(
    VertexBufferId V0,
    VertexBufferId V1,
    VertexBufferId V2,
    VertexBufferId V3);

public sealed record MeshLayout(
    MeshId MeshId,
    IndexBufferId IboId,
    VertexBufferId[] VboIds,
    VertexAttribute[] Attributes);

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
    public readonly int Components = components;
    public readonly int Offset = offset;
    public readonly byte Binding = binding;
    public readonly byte Location = location;
    public readonly VertexFormat Format = format;
    public readonly bool Normalized = normalized;
}

public readonly record struct MeshDrawProperties(
    DrawPrimitive Primitive,
    DrawMeshKind Kind,
    DrawElementSize ElementSize,
    int DrawCount
)
{
    public static MeshDrawProperties FromMeta(in MeshMeta meta) =>
        new(meta.Primitive, meta.Kind, meta.ElementSize, meta.DrawCount);

    public static MeshDrawProperties MakeDefault() =>
        new(DrawPrimitive.Triangles, DrawMeshKind.Invalid, DrawElementSize.Invalid, 0);

    public static MeshDrawProperties MakeTriElemental(DrawMeshKind kind = DrawMeshKind.Elements,
        DrawElementSize size = DrawElementSize.UnsignedInt, int drawCount = 0) =>
        new(DrawPrimitive.Triangles, kind, size, drawCount);
}