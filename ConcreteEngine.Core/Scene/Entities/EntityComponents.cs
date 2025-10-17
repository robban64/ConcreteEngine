#region

using System.Numerics;
using ConcreteEngine.Core.Assets.Materials;
using ConcreteEngine.Core.Rendering.Data;
using ConcreteEngine.Graphics.Gfx.Resources;
using Silk.NET.Maths;

#endregion

namespace ConcreteEngine.Core.Scene.Entities;

public struct Transform2D(Vector2 position, Vector2 scale, float rotation)
{
    public Vector2 Position = position;
    public Vector2 Scale = scale;
    public float Rotation = rotation;
}

public struct Transform(Vector3 position, Vector3 scale, Quaternion rotation)
{
    public Vector3 Position = position;
    public Vector3 Scale = scale;
    public Quaternion Rotation = rotation;
}

public struct MeshComponent(MeshId meshId, MaterialId materialId, int drawCount)
{
    public MeshId MeshId = meshId;
    public MaterialId MaterialId = materialId;
    public int DrawCount = drawCount;
}

public struct SpriteComponent(int spriteId, MaterialId materialId, bool isStatic)
{
    public Vector2 UvScale;
    public int SpriteId = spriteId;
    public MaterialId MaterialId = materialId;
    public Rectangle<int> SourceRectangle;
    public bool IsStatic = isStatic;
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