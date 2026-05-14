using ConcreteEngine.Core.Common;
using ConcreteEngine.Core.Engine;
using ConcreteEngine.Core.Engine.Assets;
using ConcreteEngine.Core.Engine.ECS;
using ConcreteEngine.Core.Engine.ECS.GameComponent;
using ConcreteEngine.Core.Engine.ECS.RenderComponent;
using ConcreteEngine.Core.Engine.Graphics;
using ConcreteEngine.Core.Engine.Scene;
using ConcreteEngine.Engine.Render;
using ConcreteEngine.Renderer.Buffer;

namespace ConcreteEngine.Engine.Scene;

internal sealed class EngineBlueprintFactory(
    AssetStore assetStore,
    MaterialStore materialStore,
    EngineRenderSystem renderSystem) : BlueprintFactory
{
    private static RenderEntityCore RenderEcs => Ecs.Render.Core;
    private static GameEntityCore GameEcs => Ecs.Game.Core;

    public override SceneObject BuildSceneObject(SceneObjectId id, SceneObjectTemplate tp)
    {
        ArgumentNullException.ThrowIfNull(tp);
        ArgumentNullException.ThrowIfNull(tp.Blueprints);

        var sceneObject = new SceneObject(id, tp.GId, tp.Name, tp.Enabled, in tp.Transform, in tp.Bounds);

        foreach (var it in tp.Blueprints)
        {
            var instance = it switch
            {
                ModelBlueprint model => BuildModel(sceneObject, model),
                ParticleBlueprint particle => BuildParticle(particle),
                _ => Throwers.Unreachable<BlueprintInstance>(nameof(tp.Blueprints))
            };
            
            sceneObject.AddInstance(instance);
        }

        return sceneObject;
    }

    private ModelInstance BuildModel(SceneObject sceneObject, ModelBlueprint bp)
    {
        var model = assetStore.Get<Model>(bp.ModelId);
        
        if (string.IsNullOrEmpty(bp.DisplayName)) 
            bp.DisplayName = model.Name;
        
        if (sceneObject.Transform.GetBounds().IsIdentity) 
            sceneObject.Transform.SetBounds(in model.Bounds);

        var instance = new ModelInstance(bp, model);
        for (int i = 0; i < model.Meshes.Length; i++)
        {
            var mesh = model.Meshes[i];
            if (mesh == null!) Throwers.NotFoundBy("Mesh not found", i);

            var materialId = i < bp.Materials.Length ? bp.Materials[i] : materialStore.FallbackMaterial.MaterialId;
            var material = materialStore.Get(materialId);
            instance.Materials.Add(material);
        }

        BuildModelEntities(instance);

        if (model.Animation != null)
            BuildAnimationEntities(instance, model.Animation);
        
        return instance;
    }

    private static void BuildModelEntities(ModelInstance component)
    {
        const EntitySourceKind kind = EntitySourceKind.Model;

        var meshes = component.Asset.Meshes;
        for (int i = 0; i < meshes.Length; i++)
        {
            var mesh = meshes[i];
            var material = component.Materials[i];

            var queue = material.Transparency ? DrawCommandQueue.Transparent : DrawCommandQueue.Opaque;
            var mask = material.HasShadowMap ? PassMask.Default : PassMask.Main;
            var meshIdx = mesh.Info.MeshIndex;
            var source = new SourceComponent(mesh.MeshId,material.MaterialId,meshIdx,kind,queue,mask);

            var entity = RenderEcs.AddEntity(source, in component.LocalTransform, in mesh.LocalBounds);
            component.RenderEntityIds.Add(entity);
        }
    }

    private static void BuildAnimationEntities(ModelInstance instance, ModelAnimation animation)
    {
        var clip = animation.Clips[0];
        var component = new RenderAnimationComponent(animation.AnimationId);
        var renderEntityIds = instance.GetRenderEntities();
        for (var i = 0; i < renderEntityIds.Length; i++)
        {
            var renderEntity = renderEntityIds[i];
            Ecs.GetRenderStore<RenderAnimationComponent>().Add(renderEntity, component);

            var gameEntity = GameEcs.AddEntity();

            instance.GameEntityIds.Add(gameEntity);

            var gameComponent = new AnimationComponent { Duration = clip.Duration, Speed = clip.TicksPerSecond };
            Ecs.GetGameStore<AnimationComponent>().Add(gameEntity, gameComponent);
            Ecs.GetGameStore<RenderLink>().Add(gameEntity, new RenderLink(renderEntity));
        }
    }


    private ParticleInstance BuildParticle(ParticleBlueprint bp)
    {
        ArgumentNullException.ThrowIfNull(bp);
        ArgumentException.ThrowIfNullOrEmpty(bp.EmitterName);

        if (string.IsNullOrEmpty(bp.DisplayName)) bp.DisplayName = bp.EmitterName;

        if (!renderSystem.Particles.TryGetEmitter(bp.EmitterName, out var emitter))
        {
            emitter = renderSystem.Particles
                .CreateEmitter(bp.EmitterName, bp.ParticleCount, in bp.Definition, in bp.State, in bp.VisualParams);
        }

        var source = new SourceComponent(default, bp.MaterialId, 0, EntitySourceKind.Particle,
            DrawCommandQueue.Particles, PassMask.Main);
        
        var transform = ParticleBlueprint.MakeTransform(bp);
        emitter.Translation = transform.Translation;

        var entity = RenderEcs.AddEntity(source, in transform, in bp.Bounds);

        var particle = new ParticleComponent(emitter.Id, bp.MaterialId);
        Ecs.GetRenderStore<ParticleComponent>().Add(entity, in particle);

        var instance = new ParticleInstance(bp, emitter);
        instance.RenderEntityIds.Add(entity);
        return instance;
    }
}