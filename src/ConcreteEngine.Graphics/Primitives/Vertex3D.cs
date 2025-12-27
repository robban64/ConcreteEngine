using System.Numerics;
using System.Runtime.InteropServices;
using ConcreteEngine.Core.Common.Numerics;

namespace ConcreteEngine.Graphics.Primitives;

[StructLayout(LayoutKind.Sequential)]
public struct Vertex3D(Vector3 position, Vector2 texCoords, Vector3 normal, Vector3 tangent)
{
    public Vector3 Position = position;
    public Vector2 TexCoords = texCoords;
    public Vector3 Normal = normal;
    public Vector3 Tangent = tangent;
}

[StructLayout(LayoutKind.Sequential)]
public struct Vertex3DSkinned
{
    public Vector3 Position;
    public Vector2 TexCoords;
    public Vector3 Normal;
    public Vector3 Tangent;

    public Int4 BoneIndices;
    public Vector4 BoneWeights;
}

[StructLayout(LayoutKind.Sequential)]
public struct Vertex3DBitangent
{
    public Vector3 Position;
    public Vector2 TexCoords;
    public Vector3 Normal;
    public Vector3 Tangent;
    public Vector3 Bitangent;
}