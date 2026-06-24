using ConcreteEngine.Core.Common;

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