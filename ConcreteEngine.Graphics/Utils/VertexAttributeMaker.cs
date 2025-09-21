using System.Runtime.CompilerServices;
using ConcreteEngine.Graphics.Contracts;
using ConcreteEngine.Graphics.Gfx.Internal;

namespace ConcreteEngine.Graphics.Utils;

public struct VertexAttributeMaker<TElement> where TElement : unmanaged
{
    public readonly int ElementSize => Unsafe.SizeOf<TElement>();
    public int Offset = 0;

    public VertexAttributeMaker()
    {
    }

    public VertexAttributeDesc Make<TComponent>(
        int vboBinding = 0,
        VertexFormat vertexFormat = VertexFormat.Float,
        bool norm = false) where TComponent : unmanaged
    {
        int stride = Unsafe.SizeOf<TComponent>();
        var scalar = vertexFormat.SizeInBytes();
        ArgumentOutOfRangeException.ThrowIfGreaterThan(stride, ElementSize, nameof(stride));
        ArgumentOutOfRangeException.ThrowIfGreaterThan(Offset + stride, ElementSize, nameof(Offset));
        if ((stride % scalar) != 0)
            throw new ArgumentException("Component size must be a multiple.", nameof(TComponent));

        int remaining = ElementSize - Offset;
        ArgumentOutOfRangeException.ThrowIfLessThan(remaining, stride, nameof(Offset));

        int componentCount = stride / scalar;

        var attribOffset = Offset;
        Offset += stride;
        return new VertexAttributeDesc(vboBinding, componentCount, attribOffset);
    }
}