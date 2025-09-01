using System.Numerics;
using System.Runtime.CompilerServices;
using ConcreteEngine.Core.Resources;
using ConcreteEngine.Core.Transforms;
using Silk.NET.Maths;

namespace ConcreteEngine.Core.Scene;


public readonly record struct GameEntityId(int Id) : IComparable<GameEntityId>
{
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public int CompareTo(GameEntityId other) => Id.CompareTo(other.Id);
}


public struct Transform2D(Vector2 position, Vector2 scale, float rotation)
{
    public Vector2 Position = position;
    public Vector2 Scale = scale;
    public float Rotation = rotation;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public Matrix4x4 GetTransform()
        => ModelTransform2D.CreateTransformMatrix(Position, Scale, Rotation);
}

public struct SpriteComponent(int spriteId, MaterialId materialId, bool isStatic)
{
    public int SpriteId = spriteId;
    public MaterialId MaterialId = materialId;
    public bool IsStatic = isStatic;
    public Rectangle<int> SourceRectangle;
    public Vector2 UvScale;
}


public struct TilemapComponent(MaterialId materialId, int mapSize, int tileSize)
{
    public MaterialId MaterialId = materialId;
    public int MapSize = mapSize;
    public int TileSize = tileSize;
}

public struct LightComponent
{
    public Vector2 Position;
    public Vector3 Color;
    public float Radius;
    public float Intensity;
}