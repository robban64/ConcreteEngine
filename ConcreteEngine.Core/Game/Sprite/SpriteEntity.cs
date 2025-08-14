using Silk.NET.Maths;

namespace ConcreteEngine.Core.Game.Sprite;

public class SpriteEntity
{
    public Vector2D<float> Position = Vector2D<float>.Zero;
    public Vector2D<float> Scale = Vector2D<float>.One;
    public float Rotation = 0;

    public Vector2D<int> AtlasLocation = Vector2D<int>.Zero;
}