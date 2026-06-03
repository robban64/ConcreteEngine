using ConcreteEngine.Core.Common;
using ConcreteEngine.Core.Engine.Assets;
using ConcreteEngine.Core.Engine.ECS;
using ConcreteEngine.Core.Engine.ECS.GameComponent;
using ConcreteEngine.Core.Engine.ECS.RenderComponent;
using ConcreteEngine.Core.Engine.Graphics;
using ConcreteEngine.Core.Engine.Scene;
using ConcreteEngine.Engine.Render;
using ConcreteEngine.Renderer.Buffer;

namespace ConcreteEngine.Engine;

internal sealed class EngineBlueprintFactory : BlueprintFactory
{
    
    private static RenderEntityCore RenderEcs => Ecs.Render.Core;
    private static GameEntityCore GameEcs => Ecs.Game.Core;
    private static AssetStore AssetStore => AssetStore.Instance;

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

    private static ModelInstance BuildModel(SceneObject sceneObject, ModelBlueprint bp)
    {
        var model = AssetStore.Get<Model>(bp.ModelId);

        if (string.IsNullOrEmpty(bp.DisplayName))
            bp.DisplayName = model.Name;

        if (sceneObject.Transform.GetBounds().IsIdentity)
            sceneObject.Transform.SetBounds(in model.Bounds);

        var instance = new ModelInstance(bp, model);
        for (int i = 0; i < model.Meshes.Length; i++)
        {
            var mesh = model.Meshes[i];
            if (mesh == null!) Throwers.NotFoundBy("Mesh not found", i);

            var materialId = i < bp.Materials.Length ? bp.Materials[i] : Material.FallbackMaterial.Id;
            var material = AssetStore.Get<Material>(materialId);
            instance.Materials.Add(material);
        }

        var modelRootEntity = BuildModelEntities(instance);

        if (model.Animation != null)
            BuildAnimationEntities(modelRootEntity, instance, model.Animation);

        return instance;
    }

    private static RenderEntityId BuildModelEntities(ModelInstance component)
    {
        const EntitySourceKind kind = EntitySourceKind.Model;

        var rootEntity = new RenderEntityId(0);
        var meshes = component.Asset.Meshes;
        var isAnimated = component.Asset.Animation != null;
        for (int i = 0; i < meshes.Length; i++)
        {
            var mesh = meshes[i];
            var material = component.Materials[i];

            var queue = material.HasTransparency ? DrawCommandQueue.Transparent : DrawCommandQueue.Opaque;
            var pass = material.RenderToggles.HasShadowMap ? PassMask.Default : PassMask.Main;
            var meshIdx = mesh.Info.MeshIndex;
            var source = new SourceComponent(mesh.MeshId, material.MaterialId, meshIdx, kind, queue, pass);

            ref readonly var bounds = ref (isAnimated ? ref component.LocalBounds : ref mesh.LocalBounds);
            var entity = RenderEcs.AddEntity(source, in component.LocalTransform, in bounds);
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

    private static ParticleInstance BuildParticle(ParticleBlueprint bp)
    {
        ArgumentNullException.ThrowIfNull(bp);
        ArgumentException.ThrowIfNullOrEmpty(bp.EmitterName);

        if (string.IsNullOrEmpty(bp.DisplayName)) bp.DisplayName = bp.EmitterName;

        if (!ParticleSystem.Instance.TryGetEmitter(bp.EmitterName, out var emitter))
        {
            emitter = ParticleSystem.Instance
                .CreateEmitter(bp.EmitterName, bp.ParticleCount, in bp.Definition, in bp.VisualParams);
        }

        var materialId = AssetStore.Get<Material>(bp.Material).MaterialId;

        var source = new SourceComponent(default, materialId, 0, EntitySourceKind.Particle,
            DrawCommandQueue.Particles, PassMask.Main);

        var transform = ParticleBlueprint.MakeTransform(bp);
        emitter.Direction = bp.Direction;
        emitter.Translation = transform.Translation;

        var entity = RenderEcs.AddEntity(source, in transform, in bp.Bounds);

        var particle = new ParticleComponent(emitter.Id, materialId);
        Ecs.GetRenderStore<ParticleComponent>().Add(entity, in particle);

        var instance = new ParticleInstance(bp, emitter);
        instance.RenderEntityIds.Add(entity);
        return instance;
    }
}