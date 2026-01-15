using System.Numerics;
using ConcreteEngine.Core.Common.Numerics;
using ConcreteEngine.Core.Common.Numerics.Maths;

namespace ConcreteEngine.Editor.Data;

public struct TransformStable
{
    public Vector3 Translation;
    public Vector3 Scale;
    public Quaternion Rotation;
    public Vector3 EulerAngles;

    public readonly void FillTransform(out Transform result)
    {
        result.Translation = Translation;
        result.Scale = Scale;
        result.Rotation = RotationMath.EulerDegreesToQuaternion(in EulerAngles);
    }

    public static void From(in Transform model, in Vector3 lastEuler, out TransformStable result)
    {
        result.Translation = model.Translation;
        result.Scale = model.Scale;
        result.Rotation = model.Rotation;
        result.EulerAngles = RotationMath.QuaternionToEulerDegrees(in model.Rotation, lastEuler);
    }

}