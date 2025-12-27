using System.Runtime.CompilerServices;
using ConcreteEngine.Graphics.Gfx.Contracts;
using ConcreteEngine.Graphics.Gfx.Definitions;
using ConcreteEngine.Graphics.Gfx.Internal;

namespace ConcreteEngine.Graphics.Gfx.Utility;

public struct VertexAttributeMaker
{
    private int _offset;

    public VertexAttribute Make<TComponent>(
        byte location,
        byte binding = 0,
        VertexFormat vertexFormat = VertexFormat.Float,
        bool norm = false) where TComponent : unmanaged
    {
        var stride = Unsafe.SizeOf<TComponent>();
        var scalar = vertexFormat.SizeInBytes();

        if (stride % scalar != 0)
            throw new ArgumentException("Component size must be a multiple.", nameof(TComponent));

        var componentCount = stride / scalar;

        var attribOffset = _offset;
        _offset += stride;
        return new VertexAttribute(location, binding, componentCount, attribOffset, vertexFormat);
    }
}

public struct VertexAttributeMaker<TElement> where TElement : unmanaged
{
    public readonly int ElementSize => Unsafe.SizeOf<TElement>();
    public int Offset = 0;

    public VertexAttributeMaker()
    {
    }

    public VertexAttribute Make<TComponent>(
        byte location,
        byte binding = 0,
        VertexFormat vertexFormat = VertexFormat.Float,
        bool norm = false) where TComponent : unmanaged
    {
        var stride = Unsafe.SizeOf<TComponent>();
        var scalar = vertexFormat.SizeInBytes();

        ArgumentOutOfRangeException.ThrowIfGreaterThan(stride, ElementSize, nameof(stride));
        ArgumentOutOfRangeException.ThrowIfGreaterThan(Offset + stride, ElementSize, nameof(Offset));
        if (stride % scalar != 0)
            throw new ArgumentException("Component size must be a multiple.", nameof(TComponent));

        var remaining = ElementSize - Offset;
        ArgumentOutOfRangeException.ThrowIfLessThan(remaining, stride, nameof(Offset));

        var componentCount = stride / scalar;

        var attribOffset = Offset;
        Offset += stride;
        return new VertexAttribute(location, binding, componentCount, attribOffset, vertexFormat);
    }
}