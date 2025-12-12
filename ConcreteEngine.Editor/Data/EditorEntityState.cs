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
    public EditorId Model;
    public EditorId MaterialKey;
    public EditorId ComponentRef;

    public EditorEntityState(in TransformData transform, in BoundingBox bounds)
    {
        TransformStable.MakeFrom(in transform, out Transform);
        Bounds = bounds;
    }
}

public struct EditorAnimationState
{
    public EditorId Model;
    public EditorId Animation;
    public int Clip;
    public int ClipCount;
    public float Time;
    public float Speed;
    public float Duration;
}

public struct EditorParticleState
{
    public ParticleDefinition Definition;
    public ParticleEmitterState EmitterState;
    public EditorId EmitterHandle;
}