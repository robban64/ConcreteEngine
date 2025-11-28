#region

using System.Numerics;
using System.Runtime.CompilerServices;
using ConcreteEngine.Common.Numerics;
using ConcreteEngine.Engine.Worlds.Data;
using ConcreteEngine.Shared.TransformData;

#endregion

namespace ConcreteEngine.Engine.Worlds.Entities;

public struct Transform(in Vector3 translation, in Vector3 scale, in Quaternion rotation)
{
    public static readonly Transform Identity = new(default, Vector3.One, Quaternion.Identity);

    public Vector3 Translation = translation;
    public Vector3 Scale = scale;
    public Quaternion Rotation = rotation;

    internal static ref TransformData AsData(ref Transform component) =>
        ref Unsafe.As<Transform, TransformData>(ref component);

    internal static ref Transform FromData(ref TransformData data) =>
        ref Unsafe.As<TransformData, Transform>(ref data);

    /*
    public TransformData Data = new(in translation, in scale, in rotation);

    [UnscopedRef] public ref Vector3 Translation => ref Data.Translation;
    [UnscopedRef] public ref Vector3 Scale => ref Data.Scale;
    [UnscopedRef] public ref Quaternion Rotation => ref Data.Rotation;
    */
}

public struct ModelComponent(ModelId model, int drawCount, MaterialTagKey materialTagKey)
{
    public ModelId Model = model;
    public MaterialTagKey MaterialKey = materialTagKey;
    public int DrawCount = drawCount;
}

public struct AnimationComponent(ModelId model)
{
    public ModelId Model = model;
    public int ClipIndex = 0;
    public float Time = 0f;
    public float Speed = 1f;
    public float Duration = 1f;
    
    public float AdvanceTime(float deltaTime)
    {
        Time += deltaTime * Speed;
        if(Time > Duration)
            Time = 0;

        return Time;
    }
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