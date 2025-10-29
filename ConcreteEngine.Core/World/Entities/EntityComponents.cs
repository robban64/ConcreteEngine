#region

using System.Numerics;
using ConcreteEngine.Common.Numerics;
using ConcreteEngine.Core.World.Data;
using ConcreteEngine.Renderer.Data;

#endregion

namespace ConcreteEngine.Core.World.Entities;

public struct Transform(Vector3 position, Vector3 scale, Quaternion rotation)
{
    public Vector3 Position = position;
    public Vector3 Scale = scale;
    public Quaternion Rotation = rotation;

    public Transform(Vector3 position) : this(position, Vector3.One, Quaternion.Identity)
    {
    }
}

public struct ModelComponent(ModelId model, int drawCount, MaterialTagKey materialTagKey)
{
    public ModelId Model = model;
    public int DrawCount = drawCount;
    public MaterialTagKey MaterialKey = materialTagKey;
    private int _pad;
}

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