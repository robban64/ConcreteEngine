#region

using Silk.NET.Maths;

#endregion

namespace ConcreteEngine.Core.Utils;

public sealed class SpriteAtlas(int columns, int rows)
{
    public Vector2D<float> Scale { get; } = new(1f / columns, 1f / rows);

    public Vector2D<float> GetOffset(int column, int row)
    {
        return new Vector2D<float>(Scale.X * column, Scale.Y * row);
    }
}