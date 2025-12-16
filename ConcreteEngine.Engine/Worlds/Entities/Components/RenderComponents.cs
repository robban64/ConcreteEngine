using System.Numerics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using ConcreteEngine.Common.Numerics;
using ConcreteEngine.Engine.Worlds.Data;
using ConcreteEngine.Renderer.Data;

namespace ConcreteEngine.Engine.Worlds.Entities.Components;

[StructLayout(LayoutKind.Sequential)]
public struct SourceComponent(
    ModelId model,
    MaterialTagKey materialTagKey,
    EntitySourceKind kind) : IEntityComponent
{
    public ModelId Model = model;
    public MaterialTagKey MaterialKey = materialTagKey;
    public EntitySourceKind Kind = kind;
}

[StructLayout(LayoutKind.Sequential)]
public struct ModelComponent : IEntityComponent
{
    private ModelId model;
    private MaterialTagKey materialTagKey;
}

[StructLayout(LayoutKind.Sequential)]
public struct AnimationComponent : IEntityComponent
{
    public float Time;
    public float Duration;
    public float Speed;
    public AnimationId Animation;
    public short Clip;

    public AnimationComponent(AnimationId animation, float speed, float duration)
    {
        Animation = animation;
        Clip = 0;
        Speed = speed;
        Duration = duration;
        Time = 0f;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public float AdvanceTime(float deltaTime)
    {
        Time += deltaTime * Speed;
        if (Time > Duration)
            Time = 0;

        return Time;
    }
}

[StructLayout(LayoutKind.Sequential)]
public struct ParticleComponent(int emitterHandle, MaterialId material) : IEntityComponent
{
    public int EmitterHandle = emitterHandle;
    public MaterialId Material = material;

    public static BoundingBox DefaultParticleBounds => new(new Vector3(-0.5f), new Vector3(0.5f));
}