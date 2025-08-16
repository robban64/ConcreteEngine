#region

using System.Numerics;
using System.Runtime.CompilerServices;
using ConcreteEngine.Core.Assets;
using ConcreteEngine.Core.Resources;
using Silk.NET.Maths;

#endregion

namespace ConcreteEngine.Core.Utils;

public sealed class SpriteAtlas
{
    public Vector2 Scale { get; }

    public SpriteAtlas(int columns, int rows)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(columns, nameof(columns));
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(rows, nameof(rows));
        Scale = new Vector2(1f / columns, 1f / rows);
    }

    public SpriteAtlas(int tileSize, int textureWidth, int textureHeight)
    {
        var (columns, rows) = FromTextureSize(tileSize, textureWidth, textureHeight);
        Scale = new Vector2(1f / columns, 1f / rows);
    }

    public SpriteAtlas(int tileSize, Texture2D texture)
    {
        ArgumentNullException.ThrowIfNull(texture);

        var (columns, rows) = FromTextureSize(tileSize, texture.Width, texture.Height);
        Scale = new Vector2(1f / columns, 1f / rows);
    }
    
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public Vector2 GetOffset(int column, int row) => new (Scale.X * column, Scale.Y * row);
    
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public Vector2 GetOffset(Vector2D<int> loc) => new (Scale.X * loc.X, Scale.Y * loc.Y);


    private static (int columns, int rows) FromTextureSize(int tileSize, int textureWidth, int textureHeight)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(tileSize, nameof(tileSize));
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(textureWidth, nameof(textureWidth));
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(textureHeight, nameof(textureHeight));
        ArgumentOutOfRangeException.ThrowIfGreaterThan(tileSize, textureHeight, nameof(tileSize));
        ArgumentOutOfRangeException.ThrowIfGreaterThan(tileSize, textureHeight, nameof(tileSize));

        return (textureWidth / tileSize, textureHeight / tileSize);
    }
}