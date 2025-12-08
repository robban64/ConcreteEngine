#region

using ConcreteEngine.Common.Numerics;
using ConcreteEngine.Common.Numerics.Maths;
using ConcreteEngine.Shared.World;

#endregion

namespace ConcreteEngine.Editor.Data;

public struct CameraDataState
{
    public ViewTransform Transform;
    public ProjectionInfo Projection;
    public Size2D Viewport;

    public void SetStableViewData(in ViewTransform model)
    {
        if (!VectorMath.DistanceNearlyEqual(in Transform.Translation, in model.Translation, MetricUnits.Millimeter))
            Transform.Translation = model.Translation;

        if (!VectorMath.DistanceNearlyEqual(in Transform.Scale, in model.Scale, MetricUnits.Millimeter))
            Transform.Scale = model.Scale;

        if (!YawPitch.NearlyEqual(Transform.Orientation, model.Orientation))
            Transform.Orientation = model.Orientation;
    }

    public readonly void Deconstruct(out ViewTransform transform, out ProjectionInfo projection,
        out Size2D viewport)
    {
        transform = Transform;
        projection = Projection;
        viewport = Viewport;
    }
}