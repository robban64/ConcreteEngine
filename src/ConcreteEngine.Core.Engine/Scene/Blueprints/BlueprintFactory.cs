using ConcreteEngine.Core.Common;
using ConcreteEngine.Core.Common.Numerics;
using ConcreteEngine.Core.Engine.Assets;
using ConcreteEngine.Core.Engine.ECS;
using ConcreteEngine.Core.Engine.ECS.GameComponent;
using ConcreteEngine.Core.Engine.ECS.RenderComponent;
using ConcreteEngine.Core.Engine.Graphics;
using ConcreteEngine.Renderer.Buffer;

namespace ConcreteEngine.Core.Engine.Scene;

internal static class BlueprintFactory
{
    public static void BuildRenderBlueprint(SceneObject sceneObject, RenderBlueprint bp)
    {
        var instance = bp switch
        {
            ModelBlueprint model => new ModelInstance(sceneObject, model),
            ParticleBlueprint particle => new ParticleInstance(sceneObject, particle),
            _ => Throwers.Unreachable<RenderBlueprintInstance>(nameof(bp))
        };
        sceneObject.AddInstance(instance);
        bp.AddInstance(instance);
        instance.OnCreate();
    }

    private static ModelInstance BuildModel(SceneObject sceneObject, ModelBlueprint bp)
    {
        ArgumentNullException.ThrowIfNull(bp);
        ArgumentNullException.ThrowIfNull(bp.Model);

        //sceneObject.Transform.SetBounds(in model.Bounds);
        var instance = new ModelInstance(sceneObject, bp);
        bp.AddInstance(instance);
        instance.OnCreate();
        return instance;
    }
    private static ParticleInstance BuildParticle(SceneObject sceneObject, ParticleBlueprint bp)
    {
        ArgumentNullException.ThrowIfNull(bp);
        ArgumentNullException.ThrowIfNull(bp.Emitter);
        
        var instance = new ParticleInstance(sceneObject, bp);
        bp.AddInstance(instance);
        instance.OnCreate();
        return instance;

/*
        if (!ParticleManager.Instance.TryGet(bp.EmitterName, out var emitter))
        {
            emitter = ParticleManager.Instance
                .CreateEmitter(bp.EmitterName, bp.ParticleCount, in bp.Definition, in bp.VisualParams);
        }
*/
    }
    
}