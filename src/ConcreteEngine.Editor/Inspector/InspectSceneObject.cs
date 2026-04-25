using System.Runtime.InteropServices;
using ConcreteEngine.Core.Engine.Assets;
using ConcreteEngine.Core.Engine.Graphics;
using ConcreteEngine.Core.Engine.Scene;
using ConcreteEngine.Editor.Core;

namespace ConcreteEngine.Editor.Inspector;

public sealed class InspectSceneObject
{
    public readonly SceneObject SceneObject;

    public SceneObjectId Id => SceneObject.Id;
    public SceneObjectKind Kind => SceneObject.Kind;

    public bool ShowDebugBounds;

    internal readonly InspectModelInstance? InspectModel;
    internal readonly InspectParticleInstance? InspectParticle;
    internal readonly InspectAnimationInstance? InspectAnimation;

    public InspectSceneObject(SceneObject sceneObject)
    {
        SceneObject = sceneObject;
        InspectorFieldProvider.Instance.SceneFields.Bind(this);

        if (sceneObject.Kind == SceneObjectKind.Model)
        {
            InspectModel = new InspectModelInstance(sceneObject.GetInstance<ModelInstance>());

            if (sceneObject.TryGetInstance<AnimationInstance>(out var animationInstance))
            {
            }
        }
        else if (sceneObject.Kind == SceneObjectKind.Particle)
            InspectParticle = new InspectParticleInstance(sceneObject.GetInstance<ParticleInstance>());
    }
}

internal sealed class InspectModelInstance
{
    private readonly ModelInstance _instance;

    public ReadOnlySpan<Material> GetMaterials() => CollectionsMarshal.AsSpan(_instance.Materials);

    public InspectModelInstance(ModelInstance instance)
    {
        _instance = instance;
        InspectorFieldProvider.Instance.ModelInstanceFields.Bind(instance);
    }
}

internal sealed class InspectAnimationInstance
{
    public readonly AnimationInstance Instance;

    public InspectAnimationInstance(AnimationInstance instance)
    {
        Instance = instance;
    }
}

internal sealed class InspectParticleInstance
{
    // Definition
    private readonly ParticleInstance _instance;
    private ParticleEmitter Emitter => _instance.Emitter;
    public string EmitterName => _instance.Emitter.EmitterName;


    public InspectParticleInstance(ParticleInstance instance)
    {
        _instance = instance;
        InspectorFieldProvider.Instance.ParticleInstanceFields.Bind(instance);
    }
}