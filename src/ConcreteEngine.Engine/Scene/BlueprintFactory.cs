using ConcreteEngine.Core.Engine.Assets;
using ConcreteEngine.Core.Engine.ECS;
using ConcreteEngine.Core.Engine.Scene;
using ConcreteEngine.Core.Renderer;
using ConcreteEngine.Engine.Assets;
using ConcreteEngine.Engine.ECS;
using ConcreteEngine.Engine.ECS.GameComponent;
using ConcreteEngine.Engine.ECS.RenderComponent;
using ConcreteEngine.Engine.Worlds;

namespace ConcreteEngine.Engine.Scene;

public sealed class BlueprintFactory(World world, AssetStore assetStore, MaterialStore materialStore)
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

        var instance = new ModelInstance(bp, model);
        for (int i = 0; i < model.Meshes.Length; i++)
        {
            var mesh = model.Meshes[i];
            if (mesh == null!) throw new InvalidOperationException($"Mesh not found {i}");

            var materialId = i < bp.Materials.Length ? bp.Materials[i] : new MaterialId(1);
            var material = materialStore.Get(materialId);
            instance.Materials.Add(material);
        }

        BuildModelEntities(instance);

        if (model.Animation != null)
            BuildAnimationEntities(instance, model.Animation);

        sceneObject.AddBlueprint(instance);
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

        if (!world.Particles.TryGetEmitter(bp.EmitterName, out var emitter))
        {
            emitter = world.Particles
                .CreateEmitter(bp.EmitterName, bp.ParticleCount, in bp.Definition, in bp.State);
        }

        var source = new SourceComponent(emitter.Mesh, bp.MaterialId, 0, EntitySourceKind.Particle,
            DrawCommandQueue.Particles, PassMask.Main);
        var transform = ParticleBlueprint.MakeTransform(bp);

        var entity = RenderEcs.AddEntity(source, in transform, in bp.Bounds);

        var particle = new ParticleComponent(emitter.EmitterHandle, bp.MaterialId);
        Ecs.Render.Stores<ParticleComponent>.Store.Add(entity, in particle);

        var instance = new ParticleInstance(bp);
        instance.RenderEntityIds.Add(entity);

        sceneObject.AddBlueprint(instance);
    }
/*
    private void BuildModel(SceneObject sceneObject, ModelBlueprint bp)
    {
        var model = assetStore.Get<Model>(bp.ModelId);
        var renderEntityIds = new RenderEntityId[model.Meshes.Length];
        for (int i = 0; i < model.Meshes.Length; i++)
        {
            var mesh = model.Meshes[i];
            if (mesh == null!) throw new InvalidOperationException($"Mesh not found {i}");

            var materialId = i < bp.MeshIndexToMaterial.Length ? bp.MeshIndexToMaterial[i] : new MaterialId(1);
            var material = materialStore.Get(materialId);

            var queue = material.Transparency ? DrawCommandQueue.Transparent : DrawCommandQueue.Opaque;
            var mask = material.HasShadowMap ? PassMask.Default : PassMask.Main;
            var source = new SourceComponent(
                mesh.MeshId,
                material.MaterialId,
                mesh.Info.MeshIndex,
                EntitySourceKind.Model,
                queue,
                mask);

            renderEntityIds[i] = RenderEcs.AddEntity(source, in bp.LocalTransform, in mesh.LocalBounds);
        }

        var gameEntityIds = model.Animation != null
            ? BuildAnimationEntities(model, model.Animation, renderEntityIds)
            : [];

        var component = new ModelObjectComponent(bp, model, renderEntityIds, gameEntityIds);
        sceneObject.AddComponent(component);
    }

    private GameEntityId[] BuildAnimationEntities(Model asset, ModelAnimation animation,
        RenderEntityId[] renderEntityIds)
    {
        var renderAnimationStore = Ecs.Render.Stores<RenderAnimationComponent>.Store;
        var gameAnimationStore = Ecs.Game.Stores<AnimationComponent>.Store;
        var renderLinkStore = Ecs.Game.Stores<RenderLink>.Store;

        var clip = animation.Clips[0];
        GameEntityId[] gameEntities = new GameEntityId[renderEntityIds.Length];
        for (var i = 0; i < renderEntityIds.Length; i++)
        {
            var renderEntity = renderEntityIds[i];
            renderAnimationStore.Add(renderEntity, new RenderAnimationComponent(asset.AnimationId));

            var gameEntity = gameEntities[i] = GameEcs.AddEntity();
            gameAnimationStore.Add(gameEntity,
                new AnimationComponent { Duration = clip.Duration, Speed = clip.TicksPerSecond });
            renderLinkStore.Add(gameEntity, new RenderLink(renderEntity));
        }

        return gameEntities;
    }

    private void BuildParticle(SceneObject sceneObject, ParticleBlueprint bp)
    {
        ArgumentNullException.ThrowIfNull(bp);
        ArgumentException.ThrowIfNullOrEmpty(bp.EmitterName);

        bp.DisplayName = bp.EmitterName;

        if (!world.Particles.TryGetEmitter(bp.EmitterName, out var emitter))
        {
            emitter = world.Particles
                .CreateEmitter(bp.EmitterName, bp.ParticleCount, in bp.Definition, in bp.State);
        }

        var source = new SourceComponent(emitter.Mesh, bp.MaterialId, 0, EntitySourceKind.Particle,
            DrawCommandQueue.Particles, PassMask.Main);
        var transform = ParticleBlueprint.MakeTransform(bp);

        var renderEntity = RenderEcs.AddEntity(source, in transform, in bp.Bounds);

        var particle = new ParticleComponent(emitter.EmitterHandle, bp.MaterialId);
        Ecs.Render.Stores<ParticleComponent>.Store.Add(renderEntity, in particle);

        var component = new ParticleObjectComponent(bp, [renderEntity], []);

        sceneObject.AddComponent(component);
    }
    */

/*
    private void BuildModel(SceneObject sceneObject, ModelBlueprint bp)
    {
        ArgumentNullException.ThrowIfNull(bp);
        ArgumentNullException.ThrowIfNull(bp.MeshIndexToMaterial);

        var renderEcs = Ecs.Render.Core ?? throw new InvalidOperationException(nameof(Ecs.Render.Core));
        var gameEcs = Ecs.Game.Core ?? throw new InvalidOperationException(nameof(Ecs.Game.Core));

        var model = assetStore.Get<Model>(bp.ModelId);
        bp.DisplayName = model.Name;

        Span<RenderEntityId> entityIds = stackalloc RenderEntityId[bp.MeshIndexToMaterial.Length];
        ref readonly var localTransform = ref bp.LocalTransform;

        var index = 0;
        foreach (var it in bp.MeshIndexToMaterial)
        {
            var mesh = model.Meshes[index];
            if (mesh == null!) throw new ArgumentNullException(nameof(bp.ModelId), $"Mesh not found {index}");

            var material = materialStore.Get(it);
            var queue = material.Transparency ? DrawCommandQueue.Transparent : DrawCommandQueue.Opaque;
            var mask = material.HasShadowMap ? PassMask.Default : PassMask.Main;

            var source = new SourceComponent(
                mesh.MeshId,
                material.MaterialId,
                mesh.Info.MeshIndex,
                EntitySourceKind.Model,
                queue,
                mask);

            entityIds[index++] = renderEcs.AddEntity(source, in localTransform, in mesh.LocalBounds);
        }

        sceneObject.AddRenderEntities(entityIds);

        if (model.Animation is null) return;

        var renderAnimationStore = Ecs.Render.Stores<RenderAnimationComponent>.Store;
        var gameAnimationStore = Ecs.Game.Stores<AnimationComponent>.Store;
        var renderLinkStore = Ecs.Game.Stores<RenderLink>.Store;

        var clip = model.Animation.Clips[0];
        Span<GameEntityId> gameEntities = stackalloc GameEntityId[entityIds.Length];
        for (var i = 0; i < entityIds.Length; i++)
        {
            var renderEntity = entityIds[i];
            renderAnimationStore.Add(renderEntity, new RenderAnimationComponent(model.AnimationId));

            var gameEntity = gameEntities[i] = gameEcs.AddEntity();
            gameAnimationStore.Add(gameEntity,
                new AnimationComponent { Duration = clip.Duration, Speed = clip.TicksPerSecond });
            renderLinkStore.Add(gameEntity, new RenderLink(renderEntity));
        }

        sceneObject.AddGameEntities(gameEntities);
    }

    private void BuildParticle(SceneObject sceneObject, ParticleBlueprint bp)
    {
        ArgumentNullException.ThrowIfNull(bp);
        ArgumentException.ThrowIfNullOrEmpty(bp.EmitterName);

        var renderEcs = Ecs.Render.Core ?? throw new InvalidOperationException(nameof(Ecs.Render.Core));
        //var gameEcs = Ecs.Game.Core ?? throw new InvalidOperationException(nameof(Ecs.Game.Core));

        bp.DisplayName = bp.EmitterName;

        if (!world.Particles.TryGetEmitter(bp.EmitterName, out var emitter))
        {
            emitter = world.Particles
                .CreateEmitter(bp.EmitterName, bp.ParticleCount, in bp.Definition, in bp.State);
        }

        var source = new SourceComponent(emitter.Mesh, bp.MaterialId, 0, EntitySourceKind.Particle,
            DrawCommandQueue.Particles, PassMask.Main);
        var transform = ParticleBlueprint.MakeTransform(bp);

        var renderEntity = renderEcs.AddEntity(source, in transform, in bp.Bounds);
        //var gameEntity = gameEcs.AddEntity();

        var particle = new ParticleComponent(emitter.EmitterHandle, bp.MaterialId);
        Ecs.Render.Stores<ParticleComponent>.Store.Add(renderEntity, in particle);
        //Ecs.Game.Stores<ParticleRefComponent>.Store.Add(gameEntity, in particle);

        sceneObject.AddRenderEntity(renderEntity);
    }
    */
}