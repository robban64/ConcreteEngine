using System.Numerics;
using ConcreteEngine.Core.Common.Numerics;
using ConcreteEngine.Graphics.Gfx;
using ConcreteEngine.Graphics.Utility;

namespace ConcreteEngine.Graphics.Primitives;

public static class VertexAttributes
{
    public static readonly VertexAttributeDef[] Vertex3DAttributes = new VertexAttributeDef[4];
    
    public static readonly VertexAttributeDef[] MainVertexAttributes = new VertexAttributeDef[4];
    public static readonly VertexAttributeDef[] SkinnedAttributes = new VertexAttributeDef[6];

    public static ReadOnlySpan<VertexAttributeDef> GetVertex3DAttributes() => Vertex3DAttributes;

    public static ReadOnlySpan<VertexAttributeDef> GetVertex3DAttributesV2() => MainVertexAttributes;
    public static ReadOnlySpan<VertexAttributeDef> GetSkinnedAttributesV2() => SkinnedAttributes;


    internal static void Initialize()
    {
        var attribBuilder = new VertexAttributeMaker();
        Vertex3DAttributes[0] = attribBuilder.Make<Vector3>(0);
        Vertex3DAttributes[1] = attribBuilder.Make<Vector2>(1);
        Vertex3DAttributes[2] = attribBuilder.Make<Vector3>(2);
        Vertex3DAttributes[3] = attribBuilder.Make<Vector3>(3);
        attribBuilder.ResetOffset();

        attribBuilder = new VertexAttributeMaker();
        SkinnedAttributes[0] = MainVertexAttributes[0] = attribBuilder.Make<Vector3>(0, 0);
        attribBuilder.ResetOffset();
        SkinnedAttributes[1] = MainVertexAttributes[1] = attribBuilder.Make<Vector2>(1, 1);
        SkinnedAttributes[2] = MainVertexAttributes[2] = attribBuilder.Make<Vector3>(2, 1);
        SkinnedAttributes[3] = MainVertexAttributes[3] = attribBuilder.Make<Vector3>(3, 1);
        attribBuilder.ResetOffset();
        SkinnedAttributes[4] = attribBuilder.Make<uint>(4, 2, VertexFormat.UByte);
        SkinnedAttributes[5] = attribBuilder.Make<uint>(5, 2, VertexFormat.UByte, normalized: true);

    }
}