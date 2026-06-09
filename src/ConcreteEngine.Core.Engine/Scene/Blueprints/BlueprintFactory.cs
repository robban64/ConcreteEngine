using ConcreteEngine.Core.Common;
using ConcreteEngine.Core.Engine.Assets;
using ConcreteEngine.Core.Engine.ECS;
using ConcreteEngine.Core.Engine.ECS.GameComponent;
using ConcreteEngine.Core.Engine.ECS.RenderComponent;
using ConcreteEngine.Core.Engine.Graphics;
using ConcreteEngine.Renderer.Buffer;

namespace ConcreteEngine.Core.Engine.Scene;

public static class BlueprintFactory
{
    private static RenderEntityCore RenderEcs => Ecs.Render.Core;
    private static GameEntityCore GameEcs => Ecs.Game.Core;
    private static AssetStore AssetStore => AssetStore.Instance;

    public static SceneObject BuildSceneObject(SceneObjectId id, SceneObjectTemplate tp)
    {
        ArgumentNullException.ThrowIfNull(tp);
        ArgumentNullException.ThrowIfNull(tp.Blueprints);

        var sceneObject = new SceneObject(id, tp.GId, tp.Name, tp.Enabled, in tp.Transform, in tp.Bounds);

        foreach (var it in tp.Blueprints)
        {
            var instance = it switch
            {
                ModelBlueprint model => BuildModel(sceneObject, model),
                ParticleBlueprint particle => BuildParticle(sceneObject, particle),
                _ => Throwers.Unreachable<BlueprintInstance>(nameof(tp.Blueprints))
            };
            sceneObject.AddInstance(instance);
        }

        return sceneObject;
    }

    private static ModelInstance BuildModel(SceneObject sceneObject, ModelBlueprint bp)
    {
        var model = bp.GetModel();
        
        if (sceneObject.Transform.GetBounds().IsIdentity)
            sceneObject.Transform.SetBounds(in model.Bounds);

        var instance = new ModelInstance(sceneObject, bp);

        var modelRootEntity = BuildModelEntities(instance);

        if (model.Animation != null)
            BuildAnimationEntities(modelRootEntity, instance, model.Animation);

        bp.AddInstance(instance);
        return instance;
    }

    private static RenderEntityId BuildModelEntities(ModelInstance component)
    {
        var rootEntity = new RenderEntityId(0);
        var meshes = component.GetModel().Meshes;
        var isAnimated = component.GetModel().Animation != null;
        for (int i = 0; i < meshes.Length; i++)
        {
            var mesh = meshes[i];
            var material = component.Blueprint.GetMaterial(i);

            var meshIdx = mesh.Info.MeshIndex;

            //var queue = material.HasTransparency ? DrawCommandQueue.Transparent : DrawCommandQueue.Opaque;
            var source = new SourceComponent(
                mesh.MeshId, 
                material.MaterialId, 
                meshIdx, 
                EntitySourceKind.Model,
                material.State.DrawQueue, 
                material.State.PassMasks);

            var entity = RenderEcs.AddEntity(source, in component.LocalTransform);
            component.RenderEntityIds.Add(entity);

            if (i == 0) rootEntity = entity;
        }

        return rootEntity;
    }

    private static void BuildAnimationEntities(RenderEntityId rootEntity, ModelInstance instance,
        ModelAnimation animation)
    {
        var clip = animation.Clips[0];

        var renderComponent = new SkinningComponent(animation.AnimationId, instance: 0);
        var gameComponent = new AnimationComponent { Duration = clip.Duration, Speed = clip.TicksPerSecond };
        var animationStore = Ecs.GetRenderStore<SkinningComponent>();

        var existing = false;
        foreach (var query in animationStore.Query())
        {
            ref readonly var c = ref query.Component;
            if (renderComponent.AnimationId != c.AnimationId || renderComponent.Instance != c.Instance)
                continue;

            existing = true;
            rootEntity = query.Entity;
            break;
        }

        if (!existing)
        {
            animationStore.Add(rootEntity, renderComponent);

            var gameEntity = GameEcs.AddEntity();
            instance.GameEntityIds.Add(gameEntity);
            Ecs.GetGameStore<AnimationComponent>().Add(gameEntity, gameComponent);
            Ecs.GetGameStore<RenderLink>().Add(gameEntity, new RenderLink(rootEntity));
        }

        var skinLinkComponent = new SkinLinkComponent { EntityId = rootEntity };
        foreach (var entity in instance.GetRenderEntities())
        {
            Ecs.GetRenderStore<SkinLinkComponent>().Add(entity, skinLinkComponent);
        }
    }

    private static ParticleInstance BuildParticle(SceneObject sceneObject, ParticleBlueprint bp)
    {
        ArgumentNullException.ThrowIfNull(bp);
        ArgumentException.ThrowIfNullOrEmpty(bp.EmitterName);

        if (string.IsNullOrEmpty(bp.DisplayName)) bp.DisplayName = bp.EmitterName;

        if (!ParticleManager.Instance.TryGet(bp.EmitterName, out var emitter))
        {
            emitter = ParticleManager.Instance
                .CreateEmitter(bp.EmitterName, bp.ParticleCount, in bp.Definition, in bp.VisualParams);
        }

        var materialId = AssetStore.Get<Material>(bp.Material).MaterialId;

        var source = new SourceComponent(default, materialId, 0, EntitySourceKind.Particle,
            DrawCommandQueue.Particles, PassMask.Main);

        var transform = ParticleBlueprint.MakeTransform(bp);
        emitter.Direction = bp.Direction;
        emitter.Translation = transform.Translation;

        var entity = RenderEcs.AddEntity(source, in transform);

        var particle = new ParticleComponent(emitter.Id);
        Ecs.GetRenderStore<ParticleComponent>().Add(entity, in particle);

        var instance = new ParticleInstance(sceneObject, bp, emitter);
        instance.RenderEntityIds.Add(entity);
        
        bp.AddInstance(instance);
        return instance;
    }}