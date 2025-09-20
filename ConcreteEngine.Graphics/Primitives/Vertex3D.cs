#region

using System.Numerics;
using System.Runtime.InteropServices;

#endregion

namespace ConcreteEngine.Graphics.Primitives;

[StructLayout(LayoutKind.Sequential)]
public struct Vertex3D
{
    public Vector3 Position;
    public Vector2 TexCoords;
    public Vector3 Normal;
    public Vector3 Tangent;

    public Vertex3D()
    {
    }

    public Vertex3D(Vector3 position, Vector2 texCoords, Vector3 normal, Vector3 tangent)
    {
        Position = position;
        TexCoords = texCoords;
        Normal = normal;
        Tangent = tangent;
    }
}

[StructLayout(LayoutKind.Sequential)]
public readonly struct Vertex3DBitangent
{
    public readonly Vector3 Position;
    public readonly Vector2 TexCoords;
    public readonly Vector3 Normal;
    public readonly Vector3 Tangent;
    public readonly Vector3 Bitangent;
}
