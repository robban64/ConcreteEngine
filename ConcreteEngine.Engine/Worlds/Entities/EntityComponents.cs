#region

using System.Diagnostics.CodeAnalysis;
using System.Numerics;
using System.Runtime.CompilerServices;
using ConcreteEngine.Common.Numerics;
using ConcreteEngine.Engine.Worlds.Data;
using ConcreteEngine.Renderer.Data;
using ConcreteEngine.Shared.TransformData;

#endregion

namespace ConcreteEngine.Engine.Worlds.Entities;

public struct TransformComponent(in Vector3 translation, in Vector3 scale, in Quaternion rotation)
{
    public static readonly TransformComponent Baseline = new(default, Vector3.One, Quaternion.Identity);
    
    public Vector3 Translation = translation;
    public Vector3 Scale = scale;
    public Quaternion Rotation = rotation;
    
    internal static ref TransformData AsData(ref TransformComponent component) =>
        ref Unsafe.As<TransformComponent, TransformData>(ref component);

    internal static ref TransformComponent FromData(ref TransformData data) =>
        ref Unsafe.As<TransformData, TransformComponent>(ref data); 

    /*
    public TransformData Data = new(in translation, in scale, in rotation);

    [UnscopedRef] public ref Vector3 Translation => ref Data.Translation;
    [UnscopedRef] public ref Vector3 Scale => ref Data.Scale;
    [UnscopedRef] public ref Quaternion Rotation => ref Data.Rotation;
    */
}

public struct ModelComponent(ModelId model, int drawCount, MaterialTagKey materialTagKey, bool animated = false)
{
    public ModelId Model = model;
    public MaterialTagKey MaterialKey = materialTagKey;
    public int DrawCount = drawCount;
    public bool Animated = animated;
}

public struct AnimationComponent(ModelId model, int bones, int animation, float animationTime)
{
    public ModelId Model = model;
    public int Slot;
    public int Bones = bones;
    public int Animation = animation;
    public float AnimationTime = animationTime;
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