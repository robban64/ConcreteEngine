#region

using System.Numerics;
using ConcreteEngine.Common.Diagnostics;
using ConcreteEngine.Common.Numerics.Maths;
using ConcreteEngine.Shared.TransformData;

#endregion

namespace ConcreteEngine.Editor.Data;

internal struct TransformDataState
{
    public Vector3 Translation;
    public Vector3 Scale;
    public Vector3 EulerAngles;

    public void From(in TransformData model)
    {
        Translation = model.Translation;
        Scale = model.Scale;
        EulerAngles = RotationMath.QuaternionToEulerDegrees(in model.Rotation, in EulerAngles);
    }

    public void FromStable(in TransformData model)
    {
        if (!VectorMath.DistanceNearlyEqual(in Translation, in model.Translation, MetricUnits.Millimeter))
            Translation = model.Translation;

        if (!VectorMath.DistanceNearlyEqual(in Scale, in model.Scale, MetricUnits.Millimeter))
            Scale = model.Scale;

        EulerAngles = RotationMath.QuaternionToEulerDegrees(in model.Rotation, in EulerAngles);
    }
}



