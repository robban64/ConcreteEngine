#region

using ConcreteEngine.Common.Numerics;
using ConcreteEngine.Shared.TransformData;

#endregion

namespace ConcreteEngine.Editor.Data;

public enum EditorRequestAction
{
    Reload,
    Set,
    Create,
    Delete
}

public readonly record struct EditorShadowPayload(int Size, bool Enabled, EditorRequestAction RequestAction);

public readonly record struct EditorShaderPayload(string Name, EditorRequestAction RequestAction);



public readonly struct CameraEditorPayload(
    long generation,
    in ViewTransformData viewTransform,
    in ProjectionInfoData projection,
    in Size2D viewport)
{
    public readonly long Generation = generation;
    public readonly ViewTransformData ViewTransform = viewTransform;
    public readonly ProjectionInfoData Projection = projection;
    public readonly Size2D Viewport = viewport;

    public void Deconstruct(out long generation, out ViewTransformData viewTransform, out ProjectionInfoData projection,
        out Size2D viewport)
    {
        generation = Generation;
        viewTransform = ViewTransform;
        projection = Projection;
        viewport = Viewport;
    }
}