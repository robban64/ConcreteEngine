#region

using System.Numerics;
using ConcreteEngine.Core.Transforms;
using ConcreteEngine.Graphics;
using Silk.NET.Maths;

#endregion

namespace ConcreteEngine.Core.Rendering.Sprite;

public readonly struct SpriteDrawData(
    Vector2 position,
    Vector2 scale,
    Vector2 textureOffset,
    Vector2 textureScale)
{
    public readonly Vector2 Position = position;
    public readonly Vector2 Scale = scale;
    public readonly Vector2 TextureOffset = textureOffset;
    public readonly Vector2 TextureScale = textureScale;

    public static SpriteDrawData From(Transform2D transform)
    {
        return new SpriteDrawData(transform.Position, transform.Scale, Vector2.Zero, Vector2.One);
    }

    public static SpriteDrawData From(Transform2D transform, Vector2 textureOffset,
        Vector2 textureScale)
    {
        return new SpriteDrawData(transform.Position, transform.Scale, textureOffset, textureScale);
    }
}