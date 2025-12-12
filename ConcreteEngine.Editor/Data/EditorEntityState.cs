#region

using ConcreteEngine.Common.Numerics;
using ConcreteEngine.Editor.Store;
using ConcreteEngine.Shared.World;

#endregion

namespace ConcreteEngine.Editor.Data;

public struct EditorEntityState
{
    public TransformStable Transform;
    public BoundingBox Bounds;
}

public struct EditorAnimationState
{
    public EditorId Model;
    public EditorId Animation;
    public int ClipIndex;
    public float Time ;
    public float Speed;
    public float Duration;
}