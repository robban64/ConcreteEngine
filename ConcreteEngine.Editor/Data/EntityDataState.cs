#region

using ConcreteEngine.Common.Numerics;
using ConcreteEngine.Shared.World;

#endregion

namespace ConcreteEngine.Editor.Data;

public struct EntityDataState
{
    public TransformStable Transform;
    public BoundingBox Bounds;
}