#region

using System.Runtime.InteropServices;
using Silk.NET.Maths;

#endregion

namespace ConcreteEngine.Graphics.Primitives;

[StructLayout(LayoutKind.Sequential)]
public readonly struct Vertex2D
{
    public readonly Vector2D<float> Position;
    public readonly Vector2D<float> Texture;

    public Vertex2D(Vector2D<float> pos, Vector2D<float> tex)
    {
        Position = pos;
        Texture = tex;
    }

    public Vertex2D(float x, float y, float ux, float uy)
    {
        Position = new Vector2D<float>(x, y);
        Texture = new Vector2D<float>(ux, uy);
    }
}