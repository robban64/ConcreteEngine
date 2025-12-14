using System.Numerics;
using System.Runtime.CompilerServices;
using Silk.NET.Maths;

namespace ConcreteEngine.Shared.Graphics;

public sealed class SpriteAtlas
{
    public int Columns { get; private set; }
    public int Rows { get; private set; }
    public int TileWidth { get; private set; }
    public int TileHeight { get; private set; }

    public Vector2 TileScale { get; private set; } // 1/cols, 1/rows
    public Vector2 InvTexSizePx { get; private set; } // 1/textureWidth, 1/textureHeight

    public SpriteAtlas()
    {
    }

    public SpriteAtlas(Vector2D<int> tileSize, Vector2D<int> textureSize)
    {
        Set(tileSize, textureSize);
    }


    public void Set(Vector2D<int> tileSize, Vector2D<int> textureSize)
    {
        TileWidth = tileSize.X;
        TileHeight = tileSize.Y;

        Columns = textureSize.X / tileSize.X;
        Rows = textureSize.Y / tileSize.Y;


        TileScale = new Vector2(1f / Columns, 1f / Rows);
        InvTexSizePx = new Vector2(1f / textureSize.X, 1f / textureSize.Y);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public Rectangle<int> At(int col, int row)
    {
        var origin = new Vector2D<int>(col * TileWidth, row * TileHeight);
        var size = new Vector2D<int>(TileWidth, TileHeight);
        return new Rectangle<int>(origin, size);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public UvRect GetUvRect(int col, int row)
    {
        return UvRect.GetInsetUv(At(col, row), InvTexSizePx);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public Rectangle<int> At(int index)
    {
        int col = index % Columns;
        int row = index / Columns;
        return At(col, row);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public UvRect GetUvRect(int index)
    {
        return UvRect.GetInsetUv(At(index), InvTexSizePx);
    }
}