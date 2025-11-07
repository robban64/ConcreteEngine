using System.Numerics;

namespace ConcreteEngine.Shared.TransformData;

public struct TransformData(in Vector3 translation, in Vector3 scale, in Quaternion rotation)
{
    public Vector3 Translation = translation;
    public Vector3 Scale = scale;
    public Quaternion Rotation = rotation;
}
