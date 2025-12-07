#region

using ConcreteEngine.Common.Numerics;
using ConcreteEngine.Common.Numerics.Maths;
using ConcreteEngine.Shared.World;

#endregion

namespace ConcreteEngine.Editor.Components.Data;

public struct CameraDataState
{
    public ViewTransformData Transform;
    public ProjectionInfoData Projection;
    public Size2D Viewport;

    public void SetStableViewData(in ViewTransformData model)
    {
        if (!VectorMath.DistanceNearlyEqual(in Transform.Translation, in model.Translation, MetricUnits.Millimeter))
            Transform.Translation = model.Translation;

        if (!VectorMath.DistanceNearlyEqual(in Transform.Scale, in model.Scale, MetricUnits.Millimeter))
            Transform.Scale = model.Scale;

        if (!YawPitch.NearlyEqual(Transform.Orientation, model.Orientation))
            Transform.Orientation = model.Orientation;
    }

    public readonly void Deconstruct(out ViewTransformData transform, out ProjectionInfoData projection,
        out Size2D viewport)
    {
        transform = Transform;
        projection = Projection;
        viewport = Viewport;
    }
}