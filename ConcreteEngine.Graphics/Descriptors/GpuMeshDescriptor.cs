#region

using ConcreteEngine.Graphics.Contracts;

#endregion

namespace ConcreteEngine.Graphics.Descriptors;


public readonly struct GpuMeshDescriptor
{
    public required MeshDrawProperties DrawProperties { get; init; }
    public required IReadOnlyList<VertexAttributeDesc> Attributes { get; init; }
/*
    public static GpuMeshDescriptor MakeArray(ReadOnlySpan<VertexAttributeDesc> atr,
        DrawPrimitive primitive, uint drawCount)
    {
        return new GpuMeshDescriptor
        {
            Attributes = atr,
            Primitive = primitive,
            DrawCount = drawCount,
            DrawKind = MeshDrawKind.Arrays,
            ElementSize = DrawElementSize.Invalid
        };
    }
    public static GpuMeshDescriptor MakeElemental(ReadOnlySpan<VertexAttributeDesc> atr,
        DrawElementSize elementSize, DrawPrimitive primitive, uint drawCount)
    {
        return new GpuMeshDescriptor
        {
            Attributes = atr,
            Primitive = primitive,
            DrawCount = drawCount,
            DrawKind = MeshDrawKind.Elements,
            ElementSize = elementSize
        };
    }*/
}

