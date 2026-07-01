using ConcreteEngine.Core.Engine.Graphics;
using ConcreteEngine.Core.Engine.Scene;

namespace ConcreteEngine.Editor.Core.Inspector;

public sealed class InspectSceneObject
{
    public readonly SceneObject SceneObject;
    public readonly SceneTransform Transform;

    public SceneObjectId Id => SceneObject.Id;
    public SceneObjectKind Kind => SceneObject.Kind;

    internal readonly InspectModelInstance? InspectModel;
    internal readonly InspectParticleInstance? InspectParticle;

    public InspectSceneObject(SceneObject sceneObject)
    {
        SceneObject = sceneObject;
        Transform = sceneObject.Transform;

        InspectorFieldProvider.Instance.SceneFields.Bind(this);

        if (sceneObject.Kind == SceneObjectKind.Model)
            InspectModel = new InspectModelInstance(sceneObject.GetInstance<ModelInstance>());
        else if (sceneObject.Kind == SceneObjectKind.Particle)
            InspectParticle = new InspectParticleInstance(sceneObject.GetInstance<ParticleInstance>());
    }
}

internal sealed class InspectModelInstance
{
    public readonly ModelInstance Instance;

    // public ReadOnlySpan<Material> GetMaterials() => CollectionsMarshal.AsSpan(Instance.Materials);

    public InspectModelInstance(ModelInstance instance)
    {
        Instance = instance;
        //InspectorFieldProvider.Instance.ModelInstanceFields.Bind(instance);
    }
}

internal sealed class InspectParticleInstance
{
    // Definition
    private readonly ParticleInstance _instance;
    private ParticleEmitter Emitter => _instance.Emitter;
    public string EmitterName => _instance.Emitter.Name;


    public InspectParticleInstance(ParticleInstance instance)
    {
        _instance = instance;
        InspectorFieldProvider.Instance.ParticleInstanceFields.Bind(instance);
    }
}