#region

using System.Numerics;
using System.Runtime.InteropServices;
using Silk.NET.Maths;

#endregion

namespace ConcreteEngine.Graphics.Primitives;

[StructLayout(LayoutKind.Sequential)]
public readonly struct Vertex3D(Vector3 pos, Vector3 tex)
{
    public readonly Vector3 Position = pos;
    public readonly Vector3 Texture = tex;
}