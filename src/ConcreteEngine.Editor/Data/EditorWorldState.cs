using ConcreteEngine.Common.Numerics;
using ConcreteEngine.Shared.World;

namespace ConcreteEngine.Editor.Data;

public struct EditorCameraState
{
    public ViewTransform Transform;
    public ProjectionInfo Projection;
    public Size2D Viewport;
}