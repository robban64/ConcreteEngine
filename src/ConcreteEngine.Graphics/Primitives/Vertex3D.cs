using System.Numerics;
using System.Runtime.InteropServices;

namespace ConcreteEngine.Graphics.Primitives;

[StructLayout(LayoutKind.Sequential)]
public struct Vertex3D(Vector3 position, Vector2 texCoords, Vector3 normal, Vector3 tangent)
{
    public Vector3 Position = position;
    public Vector2 TexCoords = texCoords;
    public Vector3 Normal = normal;
    public Vector3 Tangent = tangent;
}