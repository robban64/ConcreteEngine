#region

using System.Numerics;
using System.Runtime.InteropServices;
using Silk.NET.Maths;

#endregion

namespace ConcreteEngine.Graphics.Primitives;

[StructLayout(LayoutKind.Sequential)]
public struct Vertex3D
{
    public Vector3 Position;
    public Vector2 TexCoords;
    public Vector3 Normal;
    public Vector3 Tangent;
    public Vector3 Bitangent;
}