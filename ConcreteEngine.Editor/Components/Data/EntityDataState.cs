#region

using ConcreteEngine.Common.Numerics;
using ConcreteEngine.Editor.Store;
using ConcreteEngine.Shared.World;

#endregion

namespace ConcreteEngine.Editor.Components.Data;

public struct EntityDataState
{
    public int EntityId;
    public TransformStable Transform;
    public BoundingBox Bounds;
}

public struct EntitySelectionState
{
    public EditorId EntityId;
    public bool IsSelected;
    public bool IsDirty;
    public bool IsRequesting;
}