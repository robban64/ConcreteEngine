using System.Numerics;
using ConcreteEngine.Core.Common.Numerics;

namespace ConcreteEngine.Shared.World;

public struct ViewTransform(in Vector3 translation, YawPitch orientation)
{
    public Vector3 Translation = translation;
    public YawPitch Orientation = orientation;
}