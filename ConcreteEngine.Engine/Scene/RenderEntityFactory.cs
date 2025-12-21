using ConcreteEngine.Engine.ECS;
using ConcreteEngine.Engine.ECS.Data;
using ConcreteEngine.Engine.ECS.Definitions;
using ConcreteEngine.Engine.ECS.RenderComponent;
using ConcreteEngine.Engine.Scene.Template;
using ConcreteEngine.Engine.Worlds;
using ConcreteEngine.Engine.Worlds.Mesh;
using ConcreteEngine.Engine.Worlds.Utility;

namespace ConcreteEngine.Engine.Scene;

internal static class RenderEntityFactory
{
    internal static RenderEntityId BuildRenderEntity(SceneObject sceneObject,  World world,
        RenderEntityHub entities, RenderEntityTemplate e)
    {
        CoreComponentBundle coreComponent = default;
        ParticleEmitter? emitter = null;


        var isModel = e.Model is not null;
        var isAnimated = e.Animation is not null;
        var isParticle = e.Particle is not null;

        if (e.Spatial is { } spatial) coreComponent.Box = spatial.LocalBounds;

        if (isModel)
        {
            var materialKey = world.MaterialTableImpl.Add(MaterialTagBuilder.FromSpan(e.Model!.Materials));
            var kind = e.Animation != null ? EntitySourceKind.AnimatedModel : EntitySourceKind.Model;
            coreComponent.Source = new SourceComponent(e.Model.Model, materialKey, kind);
            sceneObject.HasModel = true;
        }
        else if (isParticle)
        {
            var particle = e.Particle!;
            if (!world.Particles.TryGetEmitter(particle.EmitterName, out emitter))
            {
                emitter = world.Particles
                    .CreateEmitter(particle.EmitterName, particle.ParticleCount, in particle.Definition);
            }

            coreComponent.Source = new SourceComponent(emitter.Model, emitter.MaterialKey, EntitySourceKind.Particle);
            sceneObject.HasParticle = true;
        }

        var entity = entities.AddEntity(in coreComponent);

        if (isAnimated)
        {
            var animation = e.Animation!;
            var component = new RenderAnimationComponent
            {
                Animation = animation.Animation,
                Clip = animation.Clip,
                Duration = animation.Duration,
                Time = animation.Time,
                Speed = animation.Speed,
            };
            entities.AddComponent(entity, component);
            sceneObject.HasAnimation = true;
        }

        if (isParticle)
        {
            var component = new ParticleComponent(emitter!.EmitterHandle, emitter.Material);
            entities.AddComponent(entity, component);
        }

        sceneObject.AddRenderEntity(entity);
        return entity;
    }
}