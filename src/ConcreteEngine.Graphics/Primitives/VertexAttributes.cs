using System.Numerics;
using ConcreteEngine.Core.Common.Numerics;
using ConcreteEngine.Graphics.Gfx;
using ConcreteEngine.Graphics.Utility;

namespace ConcreteEngine.Graphics.Primitives;

public static class VertexAttributes
{
    private static readonly VertexAttributeDef[] Vertex3DAttributes = new VertexAttributeDef[4];
    private static readonly VertexAttributeDef[] SkinnedAttributes = new VertexAttributeDef[6];
    
    private static readonly VertexAttributeDef[] Vertex3DAttributesV2 = new VertexAttributeDef[4];
    private static readonly VertexAttributeDef[] SkinnedAttributesV2 = new VertexAttributeDef[6];

    public static ReadOnlySpan<VertexAttributeDef> GetVertex3DAttributes() => Vertex3DAttributes;
    public static ReadOnlySpan<VertexAttributeDef> GetSkinnedAttributes() => SkinnedAttributes;

    public static ReadOnlySpan<VertexAttributeDef> GetVertex3DAttributesV2() => Vertex3DAttributesV2;
    public static ReadOnlySpan<VertexAttributeDef> GetSkinnedAttributesV2() => SkinnedAttributesV2;


    internal static void Initialize()
    {
        var attribBuilder = new VertexAttributeMaker();
        SkinnedAttributes[0] = Vertex3DAttributes[0] = attribBuilder.Make<Vector3>(0);
        SkinnedAttributes[1] = Vertex3DAttributes[1] = attribBuilder.Make<Vector2>(1);
        SkinnedAttributes[2] = Vertex3DAttributes[2] = attribBuilder.Make<Vector3>(2);
        SkinnedAttributes[3] = Vertex3DAttributes[3] = attribBuilder.Make<Vector3>(3);
        attribBuilder.ResetOffset();
        SkinnedAttributes[4] = attribBuilder.Make<uint>(4, 1, VertexFormat.UByte);
        SkinnedAttributes[5] = attribBuilder.Make<uint>(5, 1, VertexFormat.UByte, normalized: true);

        attribBuilder = new VertexAttributeMaker();
        SkinnedAttributesV2[0] = Vertex3DAttributesV2[0] = attribBuilder.Make<Vector3>(0, 0);
        attribBuilder.ResetOffset();
        SkinnedAttributesV2[1] = Vertex3DAttributesV2[1] = attribBuilder.Make<Vector2>(1, 1);
        SkinnedAttributesV2[2] = Vertex3DAttributesV2[2] = attribBuilder.Make<Vector3>(2, 1);
        SkinnedAttributesV2[3] = Vertex3DAttributesV2[3] = attribBuilder.Make<Vector3>(3, 1);
        attribBuilder.ResetOffset();
        SkinnedAttributesV2[4] = attribBuilder.Make<uint>(4, 2, VertexFormat.UByte);
        SkinnedAttributesV2[5] = attribBuilder.Make<uint>(5, 2, VertexFormat.UByte, normalized: true);

    }
}