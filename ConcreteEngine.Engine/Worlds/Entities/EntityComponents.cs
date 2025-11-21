#region

using System.Numerics;
using ConcreteEngine.Common.Numerics;
using ConcreteEngine.Engine.Worlds.Data;
using ConcreteEngine.Renderer.Data;

#endregion

namespace ConcreteEngine.Engine.Worlds.Entities;

public struct Transform(in Vector3 translation, in Vector3 scale, in Quaternion rotation)
{
    public static readonly Transform Baseline = new (default, Vector3.One, Quaternion.Identity);
    
    public Vector3 Translation = translation;
    public Vector3 Scale = scale;
    public Quaternion Rotation = rotation;
}

public struct ModelComponent(ModelId model, int drawCount, MaterialTagKey materialTagKey)
{
    public ModelId Model = model;
    public MaterialTagKey MaterialKey = materialTagKey;
    public int DrawCount = drawCount;
    private int _pad;
}

public struct BoxComponent(in BoundingBox box)
{
    public BoundingBox Box = box;
}

/*
public struct Transform2D(Vector2 position, Vector2 scale, float rotation)
{
    public Vector2 Position = position;
    public Vector2 Scale = scale;
    public float Rotation = rotation;
}

public struct SpriteComponent(int spriteId, MaterialId materialId, bool isStatic)
{
    public Vector2 UvScale;
    public RectF SourceRectangle;
    public int SpriteId = spriteId;
    public MaterialId MaterialId = materialId;
    public bool IsStatic = isStatic;
}
*/