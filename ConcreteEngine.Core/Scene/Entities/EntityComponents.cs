#region

using System.Numerics;
using ConcreteEngine.Common.Numerics;
using ConcreteEngine.Core.Assets.Data;
using ConcreteEngine.Core.Assets.Meshes;
using ConcreteEngine.Core.RenderingSystem;
using ConcreteEngine.Graphics.Gfx.Resources;
using ConcreteEngine.Renderer.Data;

#endregion

namespace ConcreteEngine.Core.Scene.Entities;

public struct Transform(Vector3 position, Vector3 scale, Quaternion rotation)
{
    public Vector3 Position = position;
    public Vector3 Scale = scale;
    public Quaternion Rotation = rotation;

    public Transform(Vector3 position) : this(position, Vector3.One, Quaternion.Identity)
    {
        
    }
}

public struct ModelComponent(AssetRef<Model> assetModel, ModelId model, int drawCount)
{
    public AssetId AssetModel = assetModel;
    public ModelId Model = model;
    public int DrawCount = drawCount;
    private int _pad0;
}
/*
public struct MeshComponent(MeshId meshId, MaterialId materialId, int drawCount)
{
    public MeshId MeshId = meshId;
    public MaterialId MaterialId = materialId;
    public int DrawCount = drawCount;
    private int _pad;
}*/
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