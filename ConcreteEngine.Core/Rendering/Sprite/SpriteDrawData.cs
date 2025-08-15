#region

using ConcreteEngine.Core.Transforms;
using ConcreteEngine.Graphics;
using Silk.NET.Maths;

#endregion

namespace ConcreteEngine.Core.Rendering.Sprite;

public readonly struct SpriteDrawData(
    Vector2D<float> position,
    Vector2D<float> scale,
    Vector2D<float> textureOffset,
    Vector2D<float> textureScale)
{
    public readonly Vector2D<float> Position = position;
    public readonly Vector2D<float> Scale = scale;
    public readonly Vector2D<float> TextureOffset = textureOffset;
    public readonly Vector2D<float> TextureScale = textureScale;

    public static SpriteDrawData From(Transform2D transform)
    {
        return new SpriteDrawData(transform.Position, transform.Scale, Vector2D<float>.Zero, Vector2D<float>.One);
    }

    public static SpriteDrawData From(Transform2D transform, Vector2D<float> textureOffset,
        Vector2D<float> textureScale)
    {
        return new SpriteDrawData(transform.Position, transform.Scale, textureOffset, textureScale);
    }
}