using System.Numerics;
using ConcreteEngine.Core.Common;
using ConcreteEngine.Core.Common.Numerics;
using ConcreteEngine.Core.Common.Numerics.Maths;
using ConcreteEngine.Core.Engine.ECS;
using ConcreteEngine.Core.Engine.Graphics;
using ConcreteEngine.Core.Engine.Scene;
using ConcreteEngine.Editor.Lib;

namespace ConcreteEngine.Editor.Bridge;

// TODO REMOVE
public abstract class ParticleProxy
{
    public abstract int ParticleCount { get; }
    public abstract ref ParticleState State { get; }
    public abstract ref ParticleDefinition Definition { get; }
}

public abstract class SceneObjectSpatialProxy
{
    public abstract ref Matrix4x4 GetModelMatrix(RenderEntityId entity);
}

public sealed class SceneObjectInspector
{
    public readonly SceneObject SceneObject;
    public readonly SceneObjectSpatialProxy SpatialProxy;

    public SceneObjectId Id => SceneObject.Id;
    public SceneObjectKind Kind => SceneObject.Kind;

    public readonly IComponentBlueprint[] Components;

    internal readonly FloatField<Float3Value> TranslationField;
    internal readonly FloatField<Float3Value> ScaleField;
    internal readonly FloatField<Float3Value> RotationField;

    internal readonly ParticleFields? ParticleFields;
    internal readonly AnimationFields? AnimationFields;

    public SceneObjectInspector(SceneObject sceneObject,SceneObjectSpatialProxy spatialProxy, ParticleProxy? particleProxy)
    {
        SceneObject = sceneObject;
        SpatialProxy = spatialProxy;
        Components = SceneObject.GetBlueprints().ToArray();

        if (particleProxy != null) ParticleFields = new ParticleFields(particleProxy);

        TranslationField = new FloatField<Float3Value>("Translation", FieldWidgetKind.Input,
            () => SceneObject.Translation,
            value => SceneObject.Translation = (Vector3)value
        ) { Delay = FieldGetDelay.Low, Layout = FieldLayout.Top };

        ScaleField = new FloatField<Float3Value>("Scale", FieldWidgetKind.Input,
            () => SceneObject.Scale,
            value => SceneObject.Scale = (Vector3)value
        ) { Delay = FieldGetDelay.Low, Layout = FieldLayout.Top };

        RotationField = new FloatField<Float3Value>("Rotation", FieldWidgetKind.Input,
            () => RotationMath.QuaternionToEulerDegrees(SceneObject.Rotation, default),
            value => SceneObject.Rotation = RotationMath.EulerDegreesToQuaternion((Vector3)value)
        ) { Delay = FieldGetDelay.Low, Layout = FieldLayout.Top };
    }
}

internal sealed class AnimationFields
{
    //private readonly AnimationProperty _animation;

    public readonly FloatField<Float1Value> SpeedField;
    public readonly FloatField<Float1Value> DurationField;

    public AnimationFields()
    {
        /*
        _animation = animation;
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
    public readonly ParticleProxy Particle;

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

    public ParticleFields(ParticleProxy particleProxy)
    {
        Particle = particleProxy;

        StartColorField = new ColorField("Start Color", true,
            () => Particle.Definition.StartColor,
            value => Particle.Definition.StartColor = (Color4)value) { Delay = FieldGetDelay.Medium };

        EndColorField = new ColorField("End Color", true,
            () => Particle.Definition.EndColor,
            value => Particle.Definition.EndColor = (Color4)value) { Delay = FieldGetDelay.Medium };

        SizeStartEndField = new FloatField<Float2Value>("Size Start / End", FieldWidgetKind.Drag,
            () => Particle.Definition.SizeStartEnd,
            value => Particle.Definition.SizeStartEnd = (Vector2)value)
        {
            Format = "%.3f", Delay = FieldGetDelay.Medium
        };

        GravityField = new FloatField<Float3Value>("Gravity", FieldWidgetKind.Drag,
            () => Particle.Definition.Gravity,
            value => Particle.Definition.Gravity = (Vector3)value) { Format = "%.3f", Delay = FieldGetDelay.Medium };

        DragField = new FloatField<Float1Value>("Drag", FieldWidgetKind.Drag,
            () => Particle.Definition.Drag,
            value => Particle.Definition.Drag = (float)value) { Format = "%.3f", Delay = FieldGetDelay.Medium };

        SpeedMinMaxField = new FloatField<Float2Value>("Speed Min / Max", FieldWidgetKind.Drag,
            () => Particle.Definition.SpeedMinMax,
            value => Particle.Definition.SpeedMinMax = (Vector2)value)
        {
            Format = "%.3f", Delay = FieldGetDelay.Medium
        };

        LifeMinMaxField = new FloatField<Float2Value>("Life Min / Max", FieldWidgetKind.Drag,
            () => Particle.Definition.LifeMinMax,
            value => Particle.Definition.LifeMinMax = (Vector2)value) { Format = "%.3f", Delay = FieldGetDelay.Medium };

        //
        TranslationField = new FloatField<Float3Value>("Translation", FieldWidgetKind.Drag,
            () => Particle.State.Translation,
            value => Particle.State.Translation = (Vector3)value) { Format = "%.3f", Delay = FieldGetDelay.Medium };

        StartAreaField = new FloatField<Float3Value>("Start Area", FieldWidgetKind.Drag,
            () => Particle.State.StartArea,
            value => Particle.State.StartArea = (Vector3)value) { Format = "%.3f", Delay = FieldGetDelay.Medium };

        DirectionField = new FloatField<Float3Value>("Direction", FieldWidgetKind.Drag,
            () => Particle.State.Direction,
            value => Particle.State.Direction = (Vector3)value) { Format = "%.3f", Delay = FieldGetDelay.Medium };

        SpreadField = new FloatField<Float1Value>("Spread", FieldWidgetKind.Drag,
            () => Particle.State.Spread,
            value => Particle.State.Spread = (float)value) { Format = "%.3f", Delay = FieldGetDelay.Medium };
    }
}