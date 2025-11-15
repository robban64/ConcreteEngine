#region

using ConcreteEngine.Common.Numerics;
using ConcreteEngine.Editor.Definitions;
using ConcreteEngine.Shared.TransformData;

#endregion

namespace ConcreteEngine.Editor.Data;

public sealed record EditorShadowPayload(int Size, bool Enabled, EditorRequestAction RequestAction);

public sealed record EditorShaderPayload(string Name, EditorRequestAction RequestAction);

public struct CameraEditorPayload(
    long generation,
    in ViewTransformData viewTransform,
    in ProjectionInfoData projection,
    in Size2D viewport)
{
    public long Generation = generation;
    public ViewTransformData ViewTransform = viewTransform;
    public ProjectionInfoData Projection = projection;
    public Size2D Viewport = viewport;

    public readonly void Deconstruct(out long generation, out ViewTransformData viewTransform,
        out ProjectionInfoData projection,
        out Size2D viewport)
    {
        generation = Generation;
        viewTransform = ViewTransform;
        projection = Projection;
        viewport = Viewport;
    }
}