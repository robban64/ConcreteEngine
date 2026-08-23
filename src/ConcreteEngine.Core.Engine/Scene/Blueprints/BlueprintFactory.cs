namespace ConcreteEngine.Core.Engine.Scene;

internal static class BlueprintFactory
{
    public static void BuildRenderBlueprint(SceneObject sceneObject, RenderBlueprint bp)
    {
        RenderBlueprintInstance instance = bp switch
        {
            ModelBlueprint model => new ModelInstance(sceneObject, model),
            ParticleBlueprint particle => new ParticleInstance(sceneObject, particle),
            _ => throw new ArgumentOutOfRangeException(nameof(bp))
        };
        sceneObject.AddInstance(instance);
        bp.AddInstance(instance);
        instance.OnCreate();
    }
}