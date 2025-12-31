using System.Numerics;
using System.Runtime.InteropServices;

namespace ConcreteEngine.Graphics.Primitives;

[StructLayout(LayoutKind.Sequential)]
public struct Vertex2D
{
    public Vector2 Position;
    public Vector2 TexCoords;

    public Vertex2D(Vector2 pos, Vector2 tex)
    {
        Position = pos;
        TexCoords = tex;
    }

    public Vertex2D(float x, float y, float ux, float uy)
    {
        Position = new Vector2(x, y);
        TexCoords = new Vector2(ux, uy);
    }
}