#region

using System.Numerics;
using ConcreteEngine.Common.Numerics;
using ConcreteEngine.Common.Numerics.Maths;
using ConcreteEngine.Shared.TransformData;

#endregion

namespace ConcreteEngine.Editor.DataState;

internal struct TransformDataState
{
    public Vector3 Translation;
    public Vector3 Scale;
    public Vector3 EulerAngles;
    public Quaternion Rotation;

    public readonly void Fill(out TransformData model)
    {
        model = new TransformData(in Translation, in Scale, in Rotation);
    }

    public void From(in TransformData model)
    {
        Translation = model.Translation;
        Scale = model.Scale;
        EulerAngles = RotationMath.QuaternionToEulerDegrees(in model.Rotation, in EulerAngles);
        Rotation = model.Rotation;
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