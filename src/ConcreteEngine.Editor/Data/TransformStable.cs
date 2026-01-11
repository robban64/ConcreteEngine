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

    public readonly void ApplyAsTransform(out Transform result)
    {
        result.Translation = Translation;
        result.Scale = Scale;
        result.Rotation = RotationMath.EulerDegreesToQuaternion(in EulerAngles);
    }

    public static TransformStable Make(in Transform model, in Vector3 lastEuler = default)
    {
        return new TransformStable
        {
            Translation = model.Translation,
            Scale = model.Scale,
            Rotation = model.Rotation,
            EulerAngles = RotationMath.QuaternionToEulerDegrees(in model.Rotation, lastEuler)
        };
    }


    public static void From(in Transform model, out TransformStable result)
    {
        result.Translation = model.Translation;
        result.Scale = model.Scale;
        result.Rotation = model.Rotation;
        result.EulerAngles = RotationMath.QuaternionToEulerDegrees(in model.Rotation, default);
    }

    public void FromStable(in Transform model)
    {
        if (!VectorMath.DistanceNearlyEqual(in Translation, in model.Translation, MetricUnits.Millimeter))
            Translation = model.Translation;

        if (!VectorMath.DistanceNearlyEqual(in Scale, in model.Scale, MetricUnits.Millimeter))
            Scale = model.Scale;

        EulerAngles = RotationMath.QuaternionToEulerDegrees(in model.Rotation, in EulerAngles);
        Rotation = model.Rotation;
    }
}