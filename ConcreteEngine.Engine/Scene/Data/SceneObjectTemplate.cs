using ConcreteEngine.Common.Numerics;
using ConcreteEngine.Engine.Assets.Materials;
using ConcreteEngine.Engine.Assets.Models;
using ConcreteEngine.Engine.Worlds.Data;
using ConcreteEngine.Renderer.Data;
using ConcreteEngine.Shared.World;

namespace ConcreteEngine.Engine.Scene.Data;

public interface IComponentTemplate
{
}

public sealed class SpatialTemplate : IComponentTemplate
{
    public BoundingBox LocalBounds;
}

public sealed class ModelTemplate : IComponentTemplate
{
    public ModelId Model;
    public MaterialMeta[] Materials = [];
}

public sealed class AnimationTemplate : IComponentTemplate
{
    public AnimationId Animation;
    public short Clip;
    public float Time;
    public float Duration;
    public float Speed;

    public AnimationTemplate()
    {
        
    }

    public AnimationTemplate( ModelAnimation animation)
    {
        var c = animation.ClipDataSpan[Clip];
        Animation = animation.AnimationId;
        Clip = 0;
        Time = 0;
        Duration = c.Duration;
        Speed = c.TicksPerSecond;
    }
}

public sealed class ParticleTemplate : IComponentTemplate
{
    public required string EmitterName;
    public ParticleDefinition Definition;
    public ParticleEmitterState State;
    public int ParticleCount;

    public MaterialId Material;
}

public sealed class EntityTemplate
{
    public SpatialTemplate? Spatial;
    public ModelTemplate? Model;
    public ParticleTemplate? Particle;

    public AnimationTemplate? Animation;
}

public sealed class SceneObjectTemplate
{
    public required string Name;
    public List<EntityTemplate> EntityTemplates = [];
}