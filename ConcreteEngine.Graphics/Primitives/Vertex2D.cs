#region

using System.Runtime.InteropServices;
using Silk.NET.Maths;

#endregion

namespace ConcreteEngine.Graphics.Primitives;

[StructLayout(LayoutKind.Sequential)]
public readonly struct Vertex2D(Vector2D<float> pos, Vector2D<float> tex)
{
    public readonly Vector2D<float> Position = pos;
    public readonly Vector2D<float> Texture = tex;
}