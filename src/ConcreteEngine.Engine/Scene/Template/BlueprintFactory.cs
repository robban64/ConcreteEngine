using ConcreteEngine.Core.Engine.Assets;
using ConcreteEngine.Core.Engine.Scene;
using ConcreteEngine.Core.Renderer;
using ConcreteEngine.Engine.Assets;
using ConcreteEngine.Engine.ECS;
using ConcreteEngine.Engine.ECS.Data;
using ConcreteEngine.Engine.ECS.Definitions;
using ConcreteEngine.Engine.ECS.GameComponent;
using ConcreteEngine.Engine.ECS.RenderComponent;
using ConcreteEngine.Engine.Worlds;

namespace ConcreteEngine.Engine.Scene.Template;

public sealed class BlueprintFactory(World world, AssetStore assetStore, MaterialStore materialStore)
{
    public SceneObject BuildSceneObject(SceneObjectId id, SceneObjectBlueprint bp)
    {
        ArgumentNullException.ThrowIfNull(bp);
        ArgumentNullException.ThrowIfNull(bp.Components);

        var sceneObject =
            new SceneObject(id, bp.GId, bp.Name, bp.Enabled, bp.Components, in bp.Transform, in bp.Bounds);

        foreach (var it in bp.Components)
        {
            switch (it)
            {
                case ModelBlueprint model: BuildModel(sceneObject, model); break;
                case ParticleBlueprint particle: BuildParticle(sceneObject, particle); break;
                default: throw new ArgumentException("Invalid blueprint type", nameof(bp.Components));
            }
        }

        return sceneObject;
    }

    private void BuildModel(SceneObject sceneObject, ModelBlueprint bp)
    {
        ArgumentNullException.ThrowIfNull(bp);
        ArgumentNullException.ThrowIfNull(bp.MeshIndexToMaterial);

        var renderEcs = Ecs.Render.Core ?? throw new InvalidOperationException(nameof(Ecs.Render.Core));
        var gameEcs = Ecs.Game.Core ?? throw new InvalidOperationException(nameof(Ecs.Game.Core));

        var dict = bp.MeshIndexToMaterial;

        var model = assetStore.Get<Model>(bp.ModelId);
        var meshes = model.Meshes;

        ref readonly var localTransform = ref bp.LocalTransform;
        int index = 0;

        Span<RenderEntityId> entityIds = stackalloc RenderEntityId[dict.Count];
        foreach (var it in dict)
        {
            var mesh = meshes[index];
            if (mesh == null!) throw new ArgumentNullException(nameof(bp.ModelId), $"Mesh not found {index}");

            var material = materialStore.Get(it.Value);
            var queue = material.State.Transparency ? DrawCommandQueue.Transparent : DrawCommandQueue.Opaque;
            var mask = material.TextureSlots.HasShadowMap ? PassMask.Default : PassMask.Main;
            var source = new SourceComponent(mesh.GfxId, material.Id, mesh.MeshIndex, EntitySourceKind.Model, queue,
                mask);
            var args = new RenderEntityArgs(source, in localTransform, in mesh.LocalBounds);
            entityIds[index++] = renderEcs.AddEntity(in args);
        }

        sceneObject.AddRenderEntities(entityIds);

        if (model.Animation is null) return;

        var renderAnimationStore = Ecs.Render.Stores<RenderAnimationComponent>.Store;
        var gameAnimationStore = Ecs.Game.Stores<AnimationComponent>.Store;
        var renderLinkStore = Ecs.Game.Stores<RenderLink>.Store;

        Span<GameEntityId> gameEntities = stackalloc GameEntityId[entityIds.Length];
        for (var i = 0; i < entityIds.Length; i++)
        {
            var renderEntity = entityIds[i];
            renderAnimationStore.Add(renderEntity, new RenderAnimationComponent(model.AnimationId));

            var gameEntity = gameEntities[i] = gameEcs.AddEntity();
            gameAnimationStore.Add(gameEntity, new AnimationComponent());
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

        if (!world.Particles.TryGetEmitter(bp.EmitterName, out var emitter))
        {
            emitter = world.Particles
                .CreateEmitter(bp.EmitterName, bp.ParticleCount, in bp.Definition, in bp.State);
        }

        var source = new SourceComponent(emitter.Mesh, bp.MaterialId, 0, EntitySourceKind.Particle,
            DrawCommandQueue.Particles, PassMask.Main);
        var transform = ParticleBlueprint.MakeTransform(bp);

        var args = new RenderEntityArgs(source, in transform, in bp.Bounds);
        var renderEntity = renderEcs.AddEntity(in args);
        //var gameEntity = gameEcs.AddEntity();

        var particle = new ParticleComponent(emitter.EmitterHandle, bp.MaterialId);
        Ecs.Render.Stores<ParticleComponent>.Store.Add(renderEntity, in particle);
        //Ecs.Game.Stores<ParticleRefComponent>.Store.Add(gameEntity, in particle);

        sceneObject.AddRenderEntity(renderEntity);
    }
}