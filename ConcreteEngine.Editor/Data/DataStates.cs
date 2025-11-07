#region

using System.Numerics;
using ConcreteEngine.Common.Diagnostics;
using ConcreteEngine.Common.Numerics;
using ConcreteEngine.Common.Numerics.Maths;

#endregion

namespace ConcreteEngine.Editor.Data;

internal struct TransformDataState
{
    public Vector3 Translation;
    public Vector3 Scale;
    public Vector3 EulerAngles;

    public void From(in TransformEditorModel model)
    {
        Translation = model.Translation;
        Scale = model.Scale;
        EulerAngles = RotationMath.QuaternionToEulerDegrees(in model.Rotation, in EulerAngles);
    }

    public void FromStable(in TransformEditorModel model)
    {
        if (!VectorMath.DistanceNearlyEqual(in Translation, in model.Translation, MetricUnits.Millimeter))
            Translation = model.Translation;

        if (!VectorMath.DistanceNearlyEqual(in Scale, in model.Scale, MetricUnits.Millimeter))
            Scale = model.Scale;

        EulerAngles = RotationMath.QuaternionToEulerDegrees(in model.Rotation, in EulerAngles);
    }
}

internal struct CameraTransformDataState
{
    public Vector3 Translation;
    public Vector3 Scale;
    public Vector2 Orientation;

    public void From(in ViewTransformEditorModel model)
    {
        Translation = model.Translation;
        Scale = model.Scale;
        Orientation = model.Orientation.AsVec2();
    }

    public void FromStable(in ViewTransformEditorModel model)
    {
        if (!VectorMath.DistanceNearlyEqual(in Translation, in model.Translation, MetricUnits.Millimeter))
            Translation = model.Translation;

        if (!VectorMath.DistanceNearlyEqual(in Scale, in model.Scale, MetricUnits.Millimeter))
            Scale = model.Scale;

        var orientation = YawPitch.FromVector2(Orientation);
        if (!YawPitch.NearlyEqual(orientation, model.Orientation))
            Orientation = model.Orientation.AsVec2();
    }
}

internal struct CameraProjectionState(float near, float far, float fov, float aspectRatio)
{
    public readonly float AspectRatio = aspectRatio;
    public float Fov = fov;
    public Vector2 NearFar = new(near, far);

    public ProjectionEditorModel ToModel() => new(AspectRatio, Fov, NearFar.X, NearFar.Y);

    public static CameraProjectionState FromModel(in ProjectionEditorModel model) =>
        new(model.Near, model.Far, model.Fov, model.AspectRatio);
}