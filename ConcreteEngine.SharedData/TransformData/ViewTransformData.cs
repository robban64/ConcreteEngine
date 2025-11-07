using System.Numerics;
using ConcreteEngine.Common.Numerics;

namespace ConcreteEngine.Shared.TransformData;

public readonly struct ViewTransformData(in Vector3 translation, in Vector3 scale, in YawPitch orientation)
{
    public readonly Vector3 Translation = translation;
    public readonly Vector3 Scale = scale;
    public readonly YawPitch Orientation = orientation;
}