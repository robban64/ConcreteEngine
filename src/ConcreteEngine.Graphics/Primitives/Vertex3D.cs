using System.Numerics;
using System.Runtime.InteropServices;
using ConcreteEngine.Graphics.Gfx.Contracts;
using ConcreteEngine.Graphics.Gfx.Utility;

namespace ConcreteEngine.Graphics.Primitives;

[StructLayout(LayoutKind.Sequential)]
public struct Vertex3D(Vector3 position, Vector2 texCoords, Vector3 normal = default, Vector3 tangent = default)
{
    public Vector3 Position = position;
    public Vector2 TexCoords = texCoords;
    public Vector3 Normal = normal;
    public Vector3 Tangent = tangent;

    public static VertexAttribute[] DefaultAttributes
    {
        get
        {
            if(field.Length == 4) return field;
            var attribBuilder = new VertexAttributeMaker();
            field = [
                attribBuilder.Make<Vector3>(0), attribBuilder.Make<Vector2>(1),
                attribBuilder.Make<Vector3>(2), attribBuilder.Make<Vector3>(3)
            ];
            return field;
        }
    } = [];
}