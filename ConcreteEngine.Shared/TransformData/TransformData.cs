#region

using System.Numerics;
using ConcreteEngine.Common.Numerics;

#endregion

namespace ConcreteEngine.Shared.TransformData;

public readonly struct TransformData(in Vector3 translation, in Vector3 scale, in Quaternion rotation)
{
    public readonly Vector3 Translation = translation;
    public readonly Vector3 Scale = scale;
    public readonly Quaternion Rotation = rotation;
}

public readonly struct ViewTransformData(in Vector3 translation, in Vector3 scale, in YawPitch orientation)
{
    public readonly Vector3 Translation = translation;
    public readonly Vector3 Scale = scale;
    public readonly YawPitch Orientation = orientation;
}