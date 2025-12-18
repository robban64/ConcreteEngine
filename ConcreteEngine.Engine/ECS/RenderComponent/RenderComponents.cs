using System.Numerics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using ConcreteEngine.Common.Numerics;
using ConcreteEngine.Engine.ECS.Definitions;
using ConcreteEngine.Engine.Worlds.Data;
using ConcreteEngine.Renderer.Data;

namespace ConcreteEngine.Engine.ECS.RenderComponent;

[StructLayout(LayoutKind.Sequential)]
public struct SourceComponent(
    ModelId model,
    MaterialTagKey materialTagKey,
    EntitySourceKind kind) : IRenderComponent<SourceComponent>
{
    public ModelId Model = model;
    public MaterialTagKey MaterialKey = materialTagKey;
    public EntitySourceKind Kind = kind;
}

[StructLayout(LayoutKind.Sequential)]
public struct ModelComponent : IRenderComponent<ModelComponent>
{
    public ModelId Model;
    public MaterialTagKey MaterialKey;
}

[StructLayout(LayoutKind.Sequential)]
public struct AnimationComponent : IRenderComponent<AnimationComponent>
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
public struct ParticleComponent(int emitterHandle, MaterialId material) : IRenderComponent<ParticleComponent>
{
    public int EmitterHandle = emitterHandle;
    public MaterialId Material = material;

    public static BoundingBox DefaultParticleBounds => new(new Vector3(-0.5f), new Vector3(0.5f));
}