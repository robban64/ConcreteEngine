#region

using System.Numerics;
using System.Runtime.CompilerServices;
using ConcreteEngine.Shared.World;

#endregion

namespace ConcreteEngine.Engine.Worlds.Entities.Components;

public struct Transform(in Vector3 translation, in Vector3 scale, in Quaternion rotation)
{
    public static readonly Transform Identity = new(default, Vector3.One, Quaternion.Identity);

    public Vector3 Translation = translation;
    public Quaternion Rotation = rotation;
    public Vector3 Scale = scale;

    public static implicit operator TransformData(Transform t) => new(in t.Translation, in t.Scale, in t.Rotation);
    public static implicit operator Transform(TransformData d) => new(in d.Translation, in d.Scale, in d.Rotation);

    internal static ref TransformData UnsafeAs(ref Transform component) =>
        ref Unsafe.As<Transform, TransformData>(ref component);

    internal static ref Transform UnsafeFrom(ref TransformData data) =>
        ref Unsafe.As<TransformData, Transform>(ref data);
}