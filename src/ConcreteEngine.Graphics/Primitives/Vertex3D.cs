using System.Numerics;
using System.Runtime.InteropServices;

namespace ConcreteEngine.Graphics.Primitives;

[StructLayout(LayoutKind.Sequential)]
public struct Vertex3D(Vector3 position, Vector2 texCoords, Vector3 normal = default, Vector3 tangent = default)
{
    public Vector3 Position = position;
    public Vector2 TexCoords = texCoords;
    public Vector3 Normal = normal;
    public Vector3 Tangent = tangent;
}

[StructLayout(LayoutKind.Sequential)]
public struct VertexShading(Vector2 texCoords, Vector3 normal, Vector3 tangent)
{
    public Vector2 TexCoords = texCoords;
    public Vector3 Normal = normal;
    public Vector3 Tangent = tangent;
}