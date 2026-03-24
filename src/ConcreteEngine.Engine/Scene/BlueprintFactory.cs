using ConcreteEngine.Core.Engine.Assets;
using ConcreteEngine.Core.Engine.ECS;
using ConcreteEngine.Core.Engine.ECS.GameComponent;
using ConcreteEngine.Core.Engine.ECS.RenderComponent;
using ConcreteEngine.Core.Engine.Scene;
using ConcreteEngine.Core.Renderer;
using ConcreteEngine.Engine.Assets;
using ConcreteEngine.Engine.Render;

namespace ConcreteEngine.Engine.Scene;

public sealed class BlueprintFactory(
    AssetStore assetStore,
    MaterialStore materialStore,
    EngineRenderSystem renderSystem)
{
    private static RenderEntityCore RenderEcs => Ecs.Render.Core;
    private static GameEntityCore GameEcs => Ecs.Game.Core;

    public SceneObject BuildSceneObject(SceneObjectId id, SceneObjectTemplate tp)
    {
        ArgumentNullException.ThrowIfNull(tp);
        ArgumentNullException.ThrowIfNull(tp.Blueprints);

        var sceneObject = new SceneObject(id, tp.GId, tp.Name, tp.Enabled, in tp.Transform, in tp.Bounds);

        foreach (var it in tp.Blueprints)
        {
            switch (it)
            {
                case ModelBlueprint model: BuildModel(sceneObject, model); break;
                case ParticleBlueprint particle: BuildParticle(sceneObject, particle); break;
                default: throw new ArgumentException("Invalid blueprint type", nameof(tp.Blueprints));
            }
        }

        return sceneObject;
    }

    private void BuildModel(SceneObject sceneObject, ModelBlueprint bp)
    {
        var model = assetStore.Get<Model>(bp.ModelId);
        if (string.IsNullOrEmpty(bp.DisplayName)) bp.DisplayName = model.Name;
        if (sceneObject.GetBounds().IsIdentity) sceneObject.SetBounds(in model.Bounds);

        var instance = new ModelInstance(bp, model);
        for (int i = 0; i < model.Meshes.Length; i++)
        {
            var mesh = model.Meshes[i];
            if (mesh == null!) throw new InvalidOperationException($"Mesh not found {i}");

            var materialId = i < bp.Materials.Length ? bp.Materials[i] : materialStore.FallbackMaterial.MaterialId;
            var material = materialStore.Get(materialId);
            instance.Materials.Add(material);
        }

        BuildModelEntities(instance);

        if (model.Animation != null)
            BuildAnimationEntities(instance, model.Animation);

        sceneObject.AddInstance(instance);
    }

    private void BuildModelEntities(ModelInstance component)
    {
        var meshes = component.Asset.Meshes;
        for (int i = 0; i < meshes.Length; i++)
        {
            var mesh = meshes[i];
            var material = component.Materials[i];

            var queue = material.Transparency ? DrawCommandQueue.Transparent : DrawCommandQueue.Opaque;
            var mask = material.HasShadowMap ? PassMask.Default : PassMask.Main;
            var source = new SourceComponent(
                mesh.MeshId,
                material.MaterialId,
                mesh.Info.MeshIndex,
                EntitySourceKind.Model,
                queue,
                mask);

            var entity = RenderEcs.AddEntity(source, in component.LocalTransform, in mesh.LocalBounds);
            component.RenderEntityIds.Add(entity);
        }
    }

    private void BuildAnimationEntities(ModelInstance instance, ModelAnimation animation)
    {
        var renderAnimationStore = Ecs.Render.Stores<RenderAnimationComponent>.Store;
        var gameAnimationStore = Ecs.Game.Stores<AnimationComponent>.Store;
        var renderLinkStore = Ecs.Game.Stores<RenderLink>.Store;

        var clip = animation.Clips[0];
        var renderEntityIds = instance.GetRenderEntities();
        for (var i = 0; i < renderEntityIds.Length; i++)
        {
            var renderEntity = renderEntityIds[i];
            renderAnimationStore.Add(renderEntity, new RenderAnimationComponent(animation.AnimationId));

            var gameEntity = GameEcs.AddEntity();
            instance.GameEntityIds.Add(gameEntity);
            gameAnimationStore.Add(gameEntity,
                new AnimationComponent { Duration = clip.Duration, Speed = clip.TicksPerSecond });
            renderLinkStore.Add(gameEntity, new RenderLink(renderEntity));
        }
    }


    private void BuildParticle(SceneObject sceneObject, ParticleBlueprint bp)
    {
        ArgumentNullException.ThrowIfNull(bp);
        ArgumentException.ThrowIfNullOrEmpty(bp.EmitterName);

        if (string.IsNullOrEmpty(bp.DisplayName)) bp.DisplayName = bp.EmitterName;

        if (!renderSystem.Particles.TryGetEmitter(bp.EmitterName, out var emitter))
        {
            emitter = renderSystem.Particles
                .CreateEmitter(bp.EmitterName, bp.ParticleCount, in bp.Definition, in bp.State);
        }

        var source = new SourceComponent(emitter.MeshId, bp.MaterialId, 0, EntitySourceKind.Particle,
            DrawCommandQueue.Particles, PassMask.Main);
        var transform = ParticleBlueprint.MakeTransform(bp);

        var entity = RenderEcs.AddEntity(source, in transform, in bp.Bounds);

        var particle = new ParticleComponent(emitter.EmitterHandle, bp.MaterialId);
        Ecs.Render.Stores<ParticleComponent>.Store.Add(entity, in particle);

        var instance = new ParticleInstance(bp, emitter);
        instance.RenderEntityIds.Add(entity);

        sceneObject.AddInstance(instance);
    }
}