namespace ConcreteEngine.Engine.Scene.Template;
/*
public interface IRenderComponentTemplate
{
}

public sealed class SpatialTemplate : IRenderComponentTemplate
{
    public Transform Transform;
    public BoundingBox Bounds;
}

//
public sealed class RenderModelTemplate : IRenderComponentTemplate
{
    public ModelId Model;
    public MaterialMeta[] Materials = [];
}

public sealed class RenderAnimationTemplate : IRenderComponentTemplate
{
    public AnimationId Animation;
    public short Clip;
    public float Time;
    public float Duration;
    public float Speed;

    public RenderAnimationTemplate()
    {
    }

    public RenderAnimationTemplate(ModelAnimation animation)
    {
        var c = animation.ClipDataSpan[Clip];
        Animation = animation.AnimationId;
        Clip = 0;
        Time = 0;
        Duration = c.Duration;
        Speed = c.TicksPerSecond;
    }
}

public sealed class RenderParticleTemplate : IRenderComponentTemplate
{
    public RenderParticleTemplate() { }

    public RenderParticleTemplate(in ParticleDefinition definition, in ParticleState state)
    {
        Definition = definition;
        State = state;
    }

    public required string EmitterName;
    public ParticleDefinition Definition;
    public ParticleState State;
    public int ParticleCount;

    public MaterialId Material;
}

public sealed class RenderEntityTemplate
{
    public SpatialTemplate? Spatial;
    public RenderModelTemplate? Model;
    public RenderParticleTemplate? Particle;
    public RenderAnimationTemplate? Animation;
}*/