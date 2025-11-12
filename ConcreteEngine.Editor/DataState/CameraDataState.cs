using System.Numerics;
using ConcreteEngine.Common.Numerics;
using ConcreteEngine.Common.Numerics.Maths;
using ConcreteEngine.Editor.Data;
using ConcreteEngine.Shared.TransformData;

namespace ConcreteEngine.Editor.DataState;

internal struct CameraDataState
{
    public CameraTransformDataState Transform;
    public CameraProjectionState Projection;

    public void From(ref ViewTransformData model)
    {
        Transform.Translation = model.Translation;
        Transform.Scale = model.Scale;
        Transform.Orientation = model.Orientation.AsVec2();
    }

    public readonly void Fill(long generation, Size2D viewport, out CameraEditorPayload model)
    {
        Transform.Fill(out var transform);
        Projection.Fill(out var projection);
        model = new CameraEditorPayload(generation, in transform, in projection, in viewport);
    }
}

internal struct CameraProjectionState(float near, float far, float fov, float aspectRatio)
{
    public readonly float AspectRatio = aspectRatio;
    public float Fov = fov;
    public Vector2 NearFar = new(near, far);

    public readonly void Fill(out ProjectionInfoData projection)
    {
        projection = new ProjectionInfoData(AspectRatio, Fov, NearFar.X, NearFar.Y);
    }

    public static CameraProjectionState From(in ProjectionInfoData model) =>
        new(model.Near, model.Far, model.Fov, model.AspectRatio);
}

internal struct CameraTransformDataState
{
    public Vector3 Translation;
    public Vector3 Scale;
    public Vector2 Orientation;

    public void From(in ViewTransformData model)
    {
        Translation = model.Translation;
        Scale = model.Scale;
        Orientation = model.Orientation.AsVec2();
    }

    public void FromStable(in ViewTransformData model)
    {
        if (!VectorMath.DistanceNearlyEqual(in Translation, in model.Translation, MetricUnits.Millimeter))
            Translation = model.Translation;

        if (!VectorMath.DistanceNearlyEqual(in Scale, in model.Scale, MetricUnits.Millimeter))
            Scale = model.Scale;

        var orientation = YawPitch.FromVector2(Orientation);
        if (!YawPitch.NearlyEqual(orientation, model.Orientation))
            Orientation = model.Orientation.AsVec2();
    }

    public readonly void Fill(out ViewTransformData model)
    {
        model = new ViewTransformData(in Translation, in Scale, YawPitch.FromVector2(Orientation));
    }
}