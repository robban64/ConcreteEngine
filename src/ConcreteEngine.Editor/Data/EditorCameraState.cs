using ConcreteEngine.Core.Common.Numerics;
using ConcreteEngine.Core.Renderer.Data;

namespace ConcreteEngine.Editor.Data;

public struct EditorCameraState
{
    public ViewTransform Transform;
    public ProjectionInfo Projection;
    public Size2D Viewport;
}