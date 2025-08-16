using System.Numerics;
using Silk.NET.Maths;

namespace ConcreteEngine.Core.Game.Sprite;

public class SpriteEntity
{
    public Vector2 Position = Vector2.Zero;
    public Vector2 Scale = Vector2.One;
    public float Rotation = 0;

    public Vector2D<int> AtlasLocation = Vector2D<int>.Zero;
}