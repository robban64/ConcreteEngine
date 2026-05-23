using System.Runtime.CompilerServices;
using ConcreteEngine.Graphics.Gfx.Definitions;
using ConcreteEngine.Graphics.Gfx.Internals;
using ConcreteEngine.Graphics.Gfx.Types;

namespace ConcreteEngine.Graphics.Utility;

public struct VertexAttributeMaker
{
    private int _offset;

    public void ResetOffset() => _offset = 0;

    public VertexAttributeDef Make<TComponent>(
        byte location,
        byte binding = 0,
        VertexFormat vertexFormat = VertexFormat.Float,
        bool normalized = false) where TComponent : unmanaged
    {
        var stride = Unsafe.SizeOf<TComponent>();
        var scalar = vertexFormat.SizeInBytes();

        if (stride % scalar != 0)
            throw new ArgumentException("Component size must be a multiple.", nameof(TComponent));

        var componentCount = stride / scalar;

        var attribOffset = _offset;
        _offset += stride;
        return new VertexAttributeDef(location, binding, componentCount, attribOffset, vertexFormat, normalized);
    }
}