using System.Numerics;
using ConcreteEngine.Core.Common.Numerics.Primitives;
using ConcreteEngine.Graphics.Gfx.Contracts;
using ConcreteEngine.Graphics.Gfx.Definitions;
using ConcreteEngine.Graphics.Utility;

namespace ConcreteEngine.Graphics.Primitives;

public static class VertexAttributes
{
    private static readonly VertexAttributeDef[] Vertex3DAttributes = new VertexAttributeDef[4];
    private static readonly VertexAttributeDef[] SkinnedAttributes = new VertexAttributeDef[6];

    public static ReadOnlySpan<VertexAttributeDef> GetVertex3DAttributes() => Vertex3DAttributes;
    public static ReadOnlySpan<VertexAttributeDef> GetSkinnedAttributes() => SkinnedAttributes;

    internal static void Initialize()
    {
        var attribBuilder = new VertexAttributeMaker();
        SkinnedAttributes[0] = Vertex3DAttributes[0] = attribBuilder.Make<Vector3>(0);
        SkinnedAttributes[1] = Vertex3DAttributes[1] = attribBuilder.Make<Vector2>(1);
        SkinnedAttributes[2] = Vertex3DAttributes[2] = attribBuilder.Make<Vector3>(2);
        SkinnedAttributes[3] = Vertex3DAttributes[3] = attribBuilder.Make<Vector3>(3);
        attribBuilder.ResetOffset();
        SkinnedAttributes[4] = attribBuilder.Make<Int4>(4, binding: 1, vertexFormat: VertexFormat.Integer);
        SkinnedAttributes[5] = attribBuilder.Make<Vector4>(5, binding: 1);
    }
}