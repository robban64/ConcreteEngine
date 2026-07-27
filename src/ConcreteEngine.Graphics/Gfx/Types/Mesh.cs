namespace ConcreteEngine.Graphics.Gfx;

public sealed class GfxMeshEntry(int vboCount, VertexAttributeDef[] attributes)
{
    public IndexBufferId IboId { get; internal set; }
    public readonly VertexBufferId[] VboIds = new VertexBufferId[vboCount];
    public readonly VertexAttributeDef[] Attributes = attributes;
}

public readonly struct VertexAttributeDef(
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
    uint drawCount,
    uint instanceCount = 0
)
{
    public MeshDrawProperties(
        DrawPrimitive primitive,
        DrawMeshKind kind,
        DrawElementSize elementSize,
        int drawCount,
        int instanceCount = 0) : this(primitive, kind, elementSize, (uint)drawCount, (uint)instanceCount)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(drawCount);
        ArgumentOutOfRangeException.ThrowIfNegative(instanceCount);
    }

    public uint DrawCount { get; init; } = drawCount;
    public uint InstanceCount { get; init; } = instanceCount;
    public DrawPrimitive Primitive { get; init; } = primitive;
    public DrawMeshKind Kind { get; init; } = kind;
    public DrawElementSize ElementSize { get; init; } = elementSize;

    public static MeshDrawProperties FromMeta(in MeshMeta meta) =>
        new(meta.Primitive, meta.Kind, meta.ElementSize, meta.DrawCount);

    public static MeshDrawProperties MakeArray(int drawCount = 0) =>
        new(DrawPrimitive.Triangles, DrawMeshKind.Arrays, DrawElementSize.None, drawCount);

    public static MeshDrawProperties MakeInstance(DrawPrimitive primitive, int drawCount, int instances) =>
        new(primitive, DrawMeshKind.ArraysInstanced, DrawElementSize.None, drawCount, instanceCount: instances);

    public static MeshDrawProperties MakeElementalInstance(DrawPrimitive primitive, DrawElementSize size, int drawCount,
        int instances) =>
        new(primitive, DrawMeshKind.ElementsInstanced, size, drawCount, instanceCount: instances);

    public static MeshDrawProperties MakeElemental(DrawMeshKind kind = DrawMeshKind.Elements,
        DrawElementSize size = DrawElementSize.UnsignedInt, int drawCount = 0) =>
        new(DrawPrimitive.Triangles, kind, size, drawCount);
}