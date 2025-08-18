#region

using System.Numerics;
using System.Runtime.InteropServices;

#endregion

namespace ConcreteEngine.Graphics.Primitives;

[StructLayout(LayoutKind.Sequential)]
public readonly struct Vertex2D
{
    public readonly Vector2 Position;
    public readonly Vector2 Texture;

    public Vertex2D(Vector2 pos, Vector2 tex)
    {
        Position = pos;
        Texture = tex;
    }

    public Vertex2D(float x, float y, float ux, float uy)
    {
        Position = new Vector2(x, y);
        Texture = new Vector2(ux, uy);
    }
}