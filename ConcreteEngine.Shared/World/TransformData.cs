#region

using System.Numerics;
using ConcreteEngine.Common.Numerics;

#endregion

namespace ConcreteEngine.Shared.World;

public struct TransformData(in Vector3 translation, in Vector3 scale, in Quaternion rotation)
{
    public Vector3 Translation = translation;
    public Vector3 Scale = scale;
    public Quaternion Rotation = rotation;
}

public struct ViewTransformData(in Vector3 translation, in Vector3 scale, in YawPitch orientation)
{
    public Vector3 Translation = translation;
    public Vector3 Scale = scale;
    public YawPitch Orientation = orientation;
}