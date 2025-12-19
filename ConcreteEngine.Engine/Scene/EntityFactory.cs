using ConcreteEngine.Engine.ECS;
using ConcreteEngine.Engine.ECS.Data;
using ConcreteEngine.Engine.ECS.Definitions;
using ConcreteEngine.Engine.ECS.RenderComponent;
using ConcreteEngine.Engine.Scene.Template;
using ConcreteEngine.Engine.Worlds;
using ConcreteEngine.Engine.Worlds.Objects;
using ConcreteEngine.Engine.Worlds.Utility;

namespace ConcreteEngine.Engine.Scene;

internal static class EntityFactory
{
    internal static RenderEntityId BuildRenderEntity(SceneObject sceneObject, in WorldContext ctx,
        RenderEntityHub entities, RenderEntityTemplate e)
    {
        CoreComponentBundle coreComponent = default;
        ParticleEmitter? emitter = null;

        if (e.Spatial is { } spatial) coreComponent.Box = spatial.LocalBounds;

        if (e.Model is { } model)
        {
            var materialKey = ctx.MaterialTable.Add(MaterialTagBuilder.FromSpan(model.Materials));
            var kind = e.Animation != null ? EntitySourceKind.AnimatedModel : EntitySourceKind.Model;
            coreComponent.Source = new SourceComponent(model.Model, materialKey, kind);
            sceneObject.HasModel = true;
        }

        else if (e.Particle is { } particle)
        {
            if (!ctx.Particles.TryGetEmitter(particle.EmitterName, out emitter))
            {
                emitter = ctx.Particles
                    .CreateEmitter(particle.EmitterName, particle.ParticleCount, in particle.Definition);
            }

            coreComponent.Source = new SourceComponent(emitter.Model, emitter.MaterialKey, EntitySourceKind.Particle);

            sceneObject.HasParticle = true;
        }

        var entity = entities.AddEntity(in coreComponent);

        if (e.Animation is { } animation)
        {
            if (e.Model is null) throw new InvalidOperationException();
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

        if (emitter is not null)
        {
            var component = new ParticleComponent(emitter.EmitterHandle, emitter.Material);
            entities.AddComponent(entity, component);
        }

        sceneObject.AddRenderEntity(entity);
        return entity;
    }
}