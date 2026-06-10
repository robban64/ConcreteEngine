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

        var sceneObject = new SceneObject(id, tp.GId, tp.Name, tp.Enabled);
        sceneObject.Transform.SetTransform(in tp.Transform);

        foreach (var it in tp.Blueprints)
        {
            var instance = it switch
            {
                ModelBlueprint model => BuildModel(sceneObject, model),
                ParticleBlueprint particle => BuildParticle(sceneObject, particle),
                _ => Throwers.Unreachable<RenderBlueprintInstance>(nameof(tp.Blueprints))
            };
            sceneObject.AddInstance(instance);
        }

        return sceneObject;
    }

    public static ModelInstance BuildModel(SceneObject sceneObject, ModelBlueprint bp)
    {
        var model = bp.Model;
        sceneObject.Transform.SetBounds(in model.Bounds);
        var instance = new ModelInstance(sceneObject, bp);
        bp.AddInstance(instance);
        instance.OnCreate();
        return instance;
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

        var material = AssetStore.Get<Material>(bp.Material);

        var transform = ParticleBlueprint.MakeTransform(bp);
        emitter.Direction = bp.Direction;
        emitter.Translation = transform.Translation;

        var instance = new ParticleInstance(sceneObject, bp, emitter);
        instance.ParticleMaterial = new AssetRef<Material>(material, instance);
        bp.LocalTransform = transform;
        
        bp.AddInstance(instance);
        instance.OnCreate();

        return instance;
    }
    
}