#region

using ConcreteEngine.Common.Numerics;
using ConcreteEngine.Shared.World;

#endregion

namespace ConcreteEngine.Editor.Data;

public struct CameraDataState
{
    public ViewTransform Transform;
    public ProjectionInfo Projection;
    public Size2D Viewport;
}