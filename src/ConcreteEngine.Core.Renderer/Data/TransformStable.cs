using System.Numerics;
using ConcreteEngine.Core.Common.Numerics;
using ConcreteEngine.Core.Common.Numerics.Maths;

namespace ConcreteEngine.Core.Renderer.Data;

public struct TransformStable
{
    public Vector3 Translation;
    public Vector3 Scale;
    public Quaternion Rotation;
    public Vector3 EulerAngles;

    public void ApplyRotationFromEuler() => Rotation = RotationMath.EulerDegreesToQuaternion(in EulerAngles);

    public static void MakeFrom(in Transform model, out TransformStable result)
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