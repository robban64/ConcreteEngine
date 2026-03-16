using System.Numerics;
using System.Runtime.InteropServices;
using ConcreteEngine.Core.Common.Numerics;
using ConcreteEngine.Core.Common.Numerics.Maths;
using ConcreteEngine.Core.Engine.Assets;
using ConcreteEngine.Core.Engine.Graphics;
using ConcreteEngine.Core.Engine.Scene;
using ConcreteEngine.Editor.Lib;

namespace ConcreteEngine.Editor.Bridge;

public sealed class InspectSceneObject
{
    public readonly SceneObject SceneObject;

    public SceneObjectId Id => SceneObject.Id;
    public SceneObjectKind Kind => SceneObject.Kind;

    public bool ShowDebugBounds;

    internal readonly InspectModelInstance? InspectModel;
    internal readonly SceneObjectFields SceneObjectFields;
    internal readonly ParticleFields? ParticleFields;
    internal readonly AnimationFields? AnimationFields;

    public InspectSceneObject(SceneObject sceneObject)
    {
        SceneObject = sceneObject;

        SceneObjectFields = new SceneObjectFields(sceneObject);
        if (sceneObject.Kind == SceneObjectKind.Model)
        {
            InspectModel = new InspectModelInstance(sceneObject.GetInstance<ModelInstance>());
            if(sceneObject.TryGetInstance<AnimationInstance>(out var animationInstance))
                AnimationFields = new AnimationFields(animationInstance);
        }
        else if (sceneObject.Kind == SceneObjectKind.Particle)
            ParticleFields = new ParticleFields(sceneObject.GetInstance<ParticleInstance>());
    }
}

internal sealed class SceneObjectFields
{
    private readonly SceneObject _sceneObject;
    internal readonly FloatField<Float3Value> TranslationField;
    internal readonly FloatField<Float3Value> ScaleField;
    internal readonly FloatField<Float3Value> RotationField;

    public SceneObjectFields(SceneObject sceneObject)
    {
        _sceneObject = sceneObject;

        TranslationField = new FloatField<Float3Value>("Translation", FieldWidgetKind.Input,
            () => _sceneObject.Translation,
            value => _sceneObject.Translation = (Vector3)value
        ) { Delay = FieldGetDelay.Low, Layout = FieldLayout.Top, Format = "%.3f" };

        ScaleField = new FloatField<Float3Value>("Scale", FieldWidgetKind.Input,
            () => _sceneObject.Scale,
            value => _sceneObject.Scale = (Vector3)value
        ){ Delay = FieldGetDelay.Low, Layout = FieldLayout.Top, Format = "%.3f" };

        RotationField = new FloatField<Float3Value>("Rotation", FieldWidgetKind.Input,
            () => RotationMath.QuaternionToEulerDegrees(_sceneObject.Rotation),
            value => _sceneObject.Rotation = RotationMath.EulerDegreesToQuaternion((Vector3)value)
        ) { Delay = FieldGetDelay.Low, Layout = FieldLayout.Top, Format = "%.3f" };
    }
}

internal sealed class InspectModelInstance
{
    private readonly ModelInstance _instance;
    internal readonly FloatField<Float3Value> TranslationField;
    internal readonly FloatField<Float3Value> ScaleField;
    internal readonly FloatField<Float3Value> RotationField;
    internal readonly FloatField<Float3Value> LocalBoundsMinField;
    internal readonly FloatField<Float3Value> LocalBoundsMaxField;

    public ReadOnlySpan<Material> GetMaterials() => CollectionsMarshal.AsSpan(_instance.Materials);

    public InspectModelInstance(ModelInstance instance)
    {
        _instance = instance;

        TranslationField = new FloatField<Float3Value>("Translation", FieldWidgetKind.Input,
            () => _instance.LocalTransform.Translation,
            value => _instance.LocalTransform.Translation = (Vector3)value
        ) { Delay = FieldGetDelay.Low, Layout = FieldLayout.Top, Format = "%.3f" };

        ScaleField = new FloatField<Float3Value>("Scale", FieldWidgetKind.Input,
            () => _instance.LocalTransform.Scale,
            value => _instance.LocalTransform.Scale = (Vector3)value
        ) { Delay = FieldGetDelay.Low, Layout = FieldLayout.Top, Format = "%.3f" };

        RotationField = new FloatField<Float3Value>("Rotation", FieldWidgetKind.Input,
            () => RotationMath.QuaternionToEulerDegrees(_instance.LocalTransform.Rotation),
            value => _instance.LocalTransform.Rotation = RotationMath.EulerDegreesToQuaternion((Vector3)value)
        ) { Delay = FieldGetDelay.Low, Layout = FieldLayout.Top, Format = "%.3f" };

        LocalBoundsMinField = new FloatField<Float3Value>("Min", FieldWidgetKind.Input,
            () => _instance.LocalBounds.Min,
            value => _instance.LocalBounds.Min = (Vector3)value
        ) { Delay = FieldGetDelay.Low, Layout = FieldLayout.Top, Format = "%.3f" };
        LocalBoundsMaxField = new FloatField<Float3Value>("Max", FieldWidgetKind.Input,
            () => _instance.LocalBounds.Max,
            value => _instance.LocalBounds.Max = (Vector3)value
        ) { Delay = FieldGetDelay.Low, Layout = FieldLayout.Top, Format = "%.3f" };

    }
}

internal sealed class AnimationFields
{
    public readonly AnimationInstance Instance;

    // public readonly FloatField<Float1Value> ClipField;
    //public readonly FloatField<Float1Value> SpeedField;
    //public readonly FloatField<Float1Value> DurationField;

    public AnimationFields(AnimationInstance instance)
    {
        Instance = instance;
        /*
        SpeedField = new FloatField<Float1Value>("Clip", FieldWidgetKind.Slider,
                () => 0,
                value => _animation.Speed = (float)value)
            { Format = "%.3f", Speed = 0.01f, Min = 0 };

        SpeedField = new FloatField<Float1Value>("Speed", FieldWidgetKind.Drag,
            () => _animation.Speed,
            value => _animation.Speed = (float)value)
        { Format = "%.3f", Speed = 0.01f, Min = 0 };
        DurationField = new FloatField<Float1Value>("Duration", FieldWidgetKind.Drag,
            () => _animation.Duration,
            value => _animation.Duration = (float)value)
        { Format = "%.3f", Speed = 0.01f, Min = 0 };
        */
    }
}

internal sealed class ParticleFields
{
    // Definition
    private readonly ParticleInstance _instance;
    private ParticleEmitter Emitter => _instance.Emitter;
    public string EmitterName => _instance.Emitter.EmitterName;

    public readonly IntField<Int1Value> ParticleCountField;

    public readonly ColorField StartColorField;
    public readonly ColorField EndColorField;
    public readonly FloatField<Float2Value> SizeStartEndField;
    public readonly FloatField<Float3Value> GravityField;
    public readonly FloatField<Float1Value> DragField;
    public readonly FloatField<Float2Value> SpeedMinMaxField;
    public readonly FloatField<Float2Value> LifeMinMaxField;

    // State
    public readonly FloatField<Float3Value> TranslationField;
    public readonly FloatField<Float3Value> StartAreaField;
    public readonly FloatField<Float3Value> DirectionField;
    public readonly FloatField<Float1Value> SpreadField;

    public ParticleFields(ParticleInstance instance)
    {
        _instance = instance;

        ParticleCountField = new IntField<Int1Value>("Particle Count", FieldWidgetKind.Input,
            () => Emitter.ParticleCount,
            value => Emitter.SetCount(int.Clamp((int)value, ParticleEmitter.MinCount, ParticleEmitter.MaxCount))
        ).WithProperties(FieldGetDelay.Medium, FieldLayout.Top, FieldTrigger.AfterChangeDeactive);

        StartColorField = new ColorField("Start Color", true,
            () => Emitter.GetDefinition().StartColor,
            value => Emitter.GetDefinition().StartColor = (Color4)value) { Delay = FieldGetDelay.Medium };

        EndColorField = new ColorField("End Color", true,
            () => Emitter.GetDefinition().EndColor,
            value => Emitter.GetDefinition().EndColor = (Color4)value) { Delay = FieldGetDelay.Medium };

        SizeStartEndField = new FloatField<Float2Value>("Size Start / End", FieldWidgetKind.Input,
            () => Emitter.GetDefinition().SizeStartEnd,
            value => Emitter.GetDefinition().SizeStartEnd = (Vector2)value)
        {
            Format = "%.3f", Delay = FieldGetDelay.Medium
        };

        GravityField = new FloatField<Float3Value>("Gravity", FieldWidgetKind.Input,
            () => Emitter.GetDefinition().Gravity,
            value => Emitter.GetDefinition().Gravity = (Vector3)value)
        {
            Format = "%.3f", Delay = FieldGetDelay.Medium
        };

        DragField = new FloatField<Float1Value>("Drag", FieldWidgetKind.Input,
            () => Emitter.GetDefinition().Drag,
            value => Emitter.GetDefinition().Drag = (float)value) { Format = "%.3f", Delay = FieldGetDelay.Medium };

        SpeedMinMaxField = new FloatField<Float2Value>("Speed Min / Max", FieldWidgetKind.Input,
            () => Emitter.GetDefinition().SpeedMinMax,
            value => Emitter.GetDefinition().SpeedMinMax = (Vector2)value)
        {
            Format = "%.3f", Delay = FieldGetDelay.Medium
        };

        LifeMinMaxField = new FloatField<Float2Value>("Life Min / Max", FieldWidgetKind.Input,
            () => Emitter.GetDefinition().LifeMinMax,
            value => Emitter.GetDefinition().LifeMinMax = (Vector2)value)
        {
            Format = "%.3f", Delay = FieldGetDelay.Medium
        };

        //
        TranslationField = new FloatField<Float3Value>("Local Position", FieldWidgetKind.Input,
            () => Emitter.GetState().Translation,
            value => Emitter.GetState().Translation = (Vector3)value) { Format = "%.3f", Delay = FieldGetDelay.Medium };

        StartAreaField = new FloatField<Float3Value>("Start Area", FieldWidgetKind.Input,
            () => Emitter.GetState().StartArea,
            value => Emitter.GetState().StartArea = (Vector3)value) { Format = "%.3f", Delay = FieldGetDelay.Medium };

        DirectionField = new FloatField<Float3Value>("Direction", FieldWidgetKind.Input,
            () => Emitter.GetState().Direction,
            value => Emitter.GetState().Direction = (Vector3)value) { Format = "%.3f", Delay = FieldGetDelay.Medium };

        SpreadField = new FloatField<Float1Value>("Spread", FieldWidgetKind.Input,
            () => Emitter.GetState().Spread,
            value => Emitter.GetState().Spread = (float)value) { Format = "%.3f", Delay = FieldGetDelay.Medium };
    }
}