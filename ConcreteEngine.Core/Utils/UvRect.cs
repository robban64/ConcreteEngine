using System.Numerics;

namespace ConcreteEngine.Core.Utils;

public readonly record struct UvRect(float U0, float V0, float U1, float V1)
{
    public static UvRect GetInsetUv(int col, int row, int tileSize, Vector2 scale)
    {
        var cols = (int)MathF.Round(1f / scale.X);
        var rows = (int)MathF.Round(1f / scale.Y);

        float texW = cols * tileSize;
        float texH = rows * tileSize;

        float du = scale.X;
        float dv = scale.Y;

        float insetU = 0.5f / texW; // half-texel
        float insetV = 0.5f / texH;

        float u0 = col * du + insetU;
        float v0 = row * dv + insetV;
        float u1 = (col + 1) * du - insetU;
        float v1 = (row + 1) * dv - insetV;

        return new UvRect(u0, v0, u1, v1);
    }
}
