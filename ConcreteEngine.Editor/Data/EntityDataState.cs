#region

using ConcreteEngine.Common.Numerics;
using ConcreteEngine.Editor.Store;
using ConcreteEngine.Shared.World;

#endregion

namespace ConcreteEngine.Editor.Data;

public struct EntityDataState
{
    public int EntityId;
    public TransformStable Transform;
    public BoundingBox Bounds;
}