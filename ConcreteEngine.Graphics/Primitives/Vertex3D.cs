#region

using System.Runtime.InteropServices;
using Silk.NET.Maths;

#endregion

namespace ConcreteEngine.Graphics.Primitives;

[StructLayout(LayoutKind.Sequential)]
public readonly struct Vertex3D(Vector3D<float> pos, Vector3D<float> tex)
{
    public readonly Vector3D<float> Position = pos;
    public readonly Vector3D<float> Texture = tex;
}