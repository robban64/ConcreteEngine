using ConcreteEngine.Common.Numerics;
using ConcreteEngine.Engine.Assets.Data;
using ConcreteEngine.Engine.Assets.Materials;
using ConcreteEngine.Engine.Assets.Models;
using ConcreteEngine.Engine.Worlds.Data;
using ConcreteEngine.Renderer.Data;
using ConcreteEngine.Shared.World;

namespace ConcreteEngine.Engine.Scene;

public interface IComponentTemplate
{ }

public sealed class SpatialComponentTemplate : IComponentTemplate
{
    public TransformData Transform;
    public BoundingBox Bounds;
}

public sealed class ModelComponentTemplate : IComponentTemplate
{
    public ModelId Model;
    public MaterialMeta[] Materials = [];
}

public sealed class AnimationComponentTemplate : IComponentTemplate
{
    public AnimationId Animation;
    public float Time;
    public float Duration;
    public float Speed;
    public short Clip;
}

public sealed class ParticleComponentTemplate : IComponentTemplate
{
    public required string EmitterName;
    public ParticleDefinition Definition;
    public ParticleEmitterState State;
    
    public MaterialId Material;
}

public sealed class WorldEntityTemplate
{
    public List<IComponentTemplate> Components = [];
}

public sealed class GameEntityTemplate
{
    public required string Name;
    public List<WorldEntityTemplate> EntityTemplates = [];
}