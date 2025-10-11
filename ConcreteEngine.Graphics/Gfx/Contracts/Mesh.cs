#region

using ConcreteEngine.Graphics.Resources;

#endregion

namespace ConcreteEngine.Graphics.Gfx.Contracts;

public readonly record struct MeshDrawProperties(
    DrawPrimitive Primitive,
    DrawMeshKind Kind,
    DrawElementSize ElementSize,
    int DrawCount
)
{
    public static MeshDrawProperties FromMeta(in MeshMeta meta) =>
        new(meta.Primitive, meta.Kind, meta.ElementSize, meta.DrawCount);

    public static MeshMeta ToMeta(in MeshDrawProperties props, int attributeLength) =>
        new(props.Primitive, props.Kind, props.ElementSize, attributeLength, props.DrawCount);

    public static MeshDrawProperties MakeDefault() =>
        new(DrawPrimitive.Triangles, DrawMeshKind.Invalid, DrawElementSize.Invalid, 0);

    public static MeshDrawProperties MakeTriElemental(DrawMeshKind kind = DrawMeshKind.Elements,
        DrawElementSize size = DrawElementSize.UnsignedInt, int drawCount = 0) =>
        new(DrawPrimitive.Triangles, kind, size, drawCount);
}

public readonly record struct VertexAttributeDesc(
    int VboBinding,
    int Components,
    int Offset,
    VertexFormat Format = VertexFormat.Float,
    bool Norm = false
);