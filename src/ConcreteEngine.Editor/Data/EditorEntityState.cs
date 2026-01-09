using ConcreteEngine.Core.Common.Numerics;
using ConcreteEngine.Core.Engine.Graphics;
using ConcreteEngine.Core.Renderer;
using ConcreteEngine.Core.Renderer.Data;

namespace ConcreteEngine.Editor.Data;

public struct EditorEntityState
{
    public TransformStable Transform;
    public BoundingBox Bounds;

    public EditorEntityState(in Transform transform, in BoundingBox bounds)
    {
        TransformStable.From(in transform, out Transform);
        Bounds = bounds;
    }
}

public struct EditorAnimationState
{
    public int Clip;
    public int ClipCount;
    public float Time;
    public float Speed;
    public float Duration;

    public ModelId Model;
    public AnimationId Animation;
}

public struct EditorParticleState
{
    public ParticleDefinition Definition;
    public ParticleEmitterState EmitterState;
    public int EmitterHandle;
}
