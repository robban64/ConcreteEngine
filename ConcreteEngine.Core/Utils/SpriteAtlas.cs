#region

using System.Numerics;
using System.Runtime.CompilerServices;
using Silk.NET.Maths;

#endregion

namespace ConcreteEngine.Core.Utils;

public sealed class SpriteAtlas
{
    public int Columns { get; }
    public int Rows { get; }
    public int TileSize { get; }
    public float InsetU { get; } // half-texel in UV
    public float InsetV { get; }
    public Vector2 Scale { get; } // 1/cols, 1/rows

    public SpriteAtlas(int tileSize, int textureWidth, int textureHeight)
    {
        TileSize = tileSize;
        Columns = textureWidth / tileSize;
        Rows = textureHeight / tileSize;

        Scale = new Vector2(1f / Columns, 1f / Rows);
        InsetU = 0.5f / (Columns * TileSize);
        InsetV = 0.5f / (Rows * TileSize);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public UvRect GetUvRect(int col, int row)
    {
        return UvRect.GetInsetUv(col, row, TileSize, Scale);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public UvRect GetUvRect(Vector2D<int> location)
    {
        return UvRect.GetInsetUv(location.X, location.Y, TileSize, Scale);
    }
}