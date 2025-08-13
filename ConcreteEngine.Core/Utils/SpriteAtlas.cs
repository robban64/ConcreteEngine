#region

using System.Runtime.CompilerServices;
using ConcreteEngine.Core.Assets;
using Silk.NET.Maths;

#endregion

namespace ConcreteEngine.Core.Utils;

public sealed class SpriteAtlas
{
    public Vector2D<float> Scale { get; }

    public SpriteAtlas(int columns, int rows)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(columns, nameof(columns));
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(rows, nameof(rows));
        Scale = new Vector2D<float>(1f / columns, 1f / rows);
    }

    public SpriteAtlas(int tileSize, int textureWidth, int textureHeight)
    {
        var (columns, rows) = FromTextureSize(tileSize, textureWidth, textureHeight);
        Scale = new Vector2D<float>(1f / columns, 1f / rows);
    }

    public SpriteAtlas(int tileSize, Texture2D texture)
    {
        ArgumentNullException.ThrowIfNull(texture);

        var (columns, rows) = FromTextureSize(tileSize, texture.Width, texture.Height);
        Scale = new Vector2D<float>(1f / columns, 1f / rows);
    }
    
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public Vector2D<float> GetOffset(int column, int row) => new (Scale.X * column, Scale.Y * row);

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