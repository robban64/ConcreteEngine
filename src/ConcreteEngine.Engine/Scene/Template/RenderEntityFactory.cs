namespace ConcreteEngine.Engine.Scene.Template;
/*
internal static class RenderEntityFactory
{
    internal static RenderEntityId BuildRenderEntity(SceneObject sceneObject, World world, RenderEntityTemplate e)
    {
        CoreComponentBundle coreComponent = default;
        ParticleEmitter? emitter = null;

        var isModel = e.Model is not null;
        var isAnimated = e.Animation is not null;
        var isParticle = e.Particle is not null;

        if (e.Spatial is { } spatial) coreComponent.Box = spatial.Bounds;

        if (isModel)
        {
            var materialKey = world.MaterialTable.Add(MaterialTagBuilder.FromSpan(e.Model!.Materials));
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

        //coreComponent.Transform = new RenderTransform(default, new Vector3(2), Quaternion.CreateFromYawPitchRoll(0.2f,0,0));
        coreComponent.Transform = RenderTransform.Identity;
        var entity = Ecs.Render.Core.AddEntity(in coreComponent);

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
            Ecs.Render.Stores<RenderAnimationComponent>.Store.Add(entity, component);
            sceneObject.HasAnimation = true;
        }

        if (isParticle)
        {
            var component = new ParticleComponent(emitter!.EmitterHandle, emitter.Material);
            Ecs.Render.Stores<ParticleComponent>.Store.Add(entity, component);
        }

        sceneObject.AddRenderEntity(entity);
        return entity;
    }
}*/