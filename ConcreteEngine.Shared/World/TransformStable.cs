#region

using System.Numerics;
using ConcreteEngine.Common.Numerics;
using ConcreteEngine.Common.Numerics.Maths;

#endregion

namespace ConcreteEngine.Shared.World;

public struct TransformStable
{
    public Vector3 Translation;
    public Vector3 Scale;
    public Quaternion Rotation;
    public Vector3 EulerAngles;

    public readonly void FillData(out TransformData model)
    {
        model.Translation = Translation;
        model.Scale = Scale;
        model.Rotation = Rotation;
    }

    public void From(in TransformData model)
    {
        Translation = model.Translation;
        Scale = model.Scale;
        Rotation = model.Rotation;
        EulerAngles = RotationMath.QuaternionToEulerDegrees(in model.Rotation, in EulerAngles);
    }

    public void FromStable(in TransformData model)
    {
        if (!VectorMath.DistanceNearlyEqual(in Translation, in model.Translation, MetricUnits.Millimeter))
            Translation = model.Translation;

        if (!VectorMath.DistanceNearlyEqual(in Scale, in model.Scale, MetricUnits.Millimeter))
            Scale = model.Scale;

        EulerAngles = RotationMath.QuaternionToEulerDegrees(in model.Rotation, in EulerAngles);
        Rotation = model.Rotation;
    }
}