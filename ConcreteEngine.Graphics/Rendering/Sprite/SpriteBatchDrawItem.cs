#region

using Silk.NET.Maths;

#endregion

namespace ConcreteEngine.Graphics.Rendering.Sprite;

public readonly struct SpriteBatchDrawItem
{
    public readonly Vector2D<float> Position;
    public readonly Vector2D<float> Scale;
    public readonly Vector2D<float> TextureOffset;
    public readonly Vector2D<float> TextureScale;

    public SpriteBatchDrawItem(
        Vector2D<float> position,
        Vector2D<float> scale,
        Vector2D<float> textureOffset,
        Vector2D<float> textureScale)
    {
        Position = position;
        Scale = scale;
        TextureOffset = textureOffset;
        TextureScale = textureScale;
    }

    public static SpriteBatchDrawItem From(Transform2D transform)
    {
        return new SpriteBatchDrawItem(transform.Position, transform.Scale, Vector2D<float>.Zero, Vector2D<float>.One);
    }

    public static SpriteBatchDrawItem From(Transform2D transform, Vector2D<float> textureOffset,
        Vector2D<float> textureScale)
    {
        return new SpriteBatchDrawItem(transform.Position, transform.Scale, textureOffset, textureScale);
    }
}