using System.Numerics;
using ConcreteEngine.Core.Common.Numerics;
using ConcreteEngine.Core.Common.Numerics.Maths;
using ConcreteEngine.Core.Engine.Graphics;
using ConcreteEngine.Core.Engine.Scene;
using ConcreteEngine.Editor.Lib.Field;

namespace ConcreteEngine.Editor.Inspector.Impl;

internal sealed class InspectSceneFields : InspectorFields<InspectSceneObject>
{
    public readonly FloatField<Float3Value> TranslationField;
    public readonly FloatField<Float3Value> ScaleField;
    public readonly FloatField<Float3Value> RotationField;

    protected override FieldLayout DefaultLayout => FieldLayout.Top;
    protected override FieldGetDelay DefaultDelay => FieldGetDelay.Low;

    public InspectSceneFields() : base(segmentCount: 1)
    {
        TranslationField =
            Register(new FloatField<Float3Value>("Translation", FieldWidgetKind.Input) { Format = "%.3f" });
        ScaleField = Register(new FloatField<Float3Value>("Scale", FieldWidgetKind.Input) { Format = "%.3f" });
        RotationField = Register(new FloatField<Float3Value>("Rotation", FieldWidgetKind.Input) { Format = "%.3f" });

        CreateSegment("Transform", true, 0, [TranslationField, ScaleField, RotationField]);
    }

    public override void Bind(InspectSceneObject target)
    {
        TranslationField.Bind(
            () => target.SceneObject.Translation,
            value => target.SceneObject.Translation = (Vector3)value
        );
        ScaleField.Bind(
            () => target.SceneObject.Scale,
            value => target.SceneObject.Scale = (Vector3)value
        );
        RotationField.Bind(
            () => RotationMath.QuaternionToEulerDegrees(target.SceneObject.Rotation),
            value => target.SceneObject.Rotation = RotationMath.EulerDegreesToQuaternion((Vector3)value)
        );
    }
}

internal sealed class InspectModelInstanceFields : InspectorFields<ModelInstance>
{
    public readonly FloatField<Float3Value> TranslationField;
    public readonly FloatField<Float3Value> ScaleField;
    public readonly FloatField<Float3Value> RotationField;
    public readonly FloatField<Float3Value> LocalBoundsMinField;
    public readonly FloatField<Float3Value> LocalBoundsMaxField;

    protected override FieldLayout DefaultLayout => FieldLayout.Top;
    protected override FieldGetDelay DefaultDelay => FieldGetDelay.Low;

    public InspectModelInstanceFields() : base(segmentCount: 2)
    {
        TranslationField =
            Register(new FloatField<Float3Value>("Translation", FieldWidgetKind.Input) { Format = "%.3f" });
        ScaleField = Register(new FloatField<Float3Value>("Scale", FieldWidgetKind.Input) { Format = "%.3f" });
        RotationField = Register(new FloatField<Float3Value>("Rotation", FieldWidgetKind.Input) { Format = "%.3f" });
        LocalBoundsMinField = Register(new FloatField<Float3Value>("Min", FieldWidgetKind.Input) { Format = "%.3f" });
        LocalBoundsMaxField = Register(new FloatField<Float3Value>("Max", FieldWidgetKind.Input) { Format = "%.3f" });

        CreateSegment("Transform", [TranslationField, ScaleField, RotationField]);
        CreateSegment("Bounds", [LocalBoundsMinField, LocalBoundsMaxField]);
    }

    public override void Bind(ModelInstance target)
    {
        TranslationField.Bind(
            () => target.LocalTransform.Translation,
            value => target.LocalTransform.Translation = (Vector3)value
        );
        ScaleField.Bind(
            () => target.LocalTransform.Scale,
            value => target.LocalTransform.Scale = (Vector3)value
        );
        RotationField.Bind(
            () => RotationMath.QuaternionToEulerDegrees(target.LocalTransform.Rotation),
            value => target.LocalTransform.Rotation = RotationMath.EulerDegreesToQuaternion((Vector3)value)
        );

        LocalBoundsMinField.Bind(
            () => target.LocalBounds.Min,
            value => target.LocalBounds.Min = (Vector3)value
        );
        LocalBoundsMaxField.Bind(
            () => target.LocalBounds.Max,
            value => target.LocalBounds.Max = (Vector3)value
        );
    }
}

internal sealed class InspectParticleFields : InspectorFields<ParticleInstance>
{
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

    protected override FieldLayout DefaultLayout => FieldLayout.Top;
    protected override FieldGetDelay DefaultDelay => FieldGetDelay.Low;


    public InspectParticleFields() : base(segmentCount: 2)
    {
        ParticleCountField = Register(new IntField<Int1Value>("Particle Count", FieldWidgetKind.Input)
            .WithProperties(FieldGetDelay.Medium, FieldLayout.Top, FieldTrigger.AfterChangeDeactive));

        StartColorField = Register(new ColorField("Start Color", true));

        EndColorField = Register(new ColorField("End Color", true));

        SizeStartEndField =
            Register(new FloatField<Float2Value>("Size Start / End", FieldWidgetKind.Input) { Format = "%.3f" });

        GravityField = Register(new FloatField<Float3Value>("Gravity", FieldWidgetKind.Input) { Format = "%.3f" });

        DragField = Register(new FloatField<Float1Value>("Drag", FieldWidgetKind.Input) { Format = "%.3f" });

        SpeedMinMaxField =
            Register(new FloatField<Float2Value>("Speed Min / Max", FieldWidgetKind.Input) { Format = "%.3f" });

        LifeMinMaxField =
            Register(new FloatField<Float2Value>("Life Min / Max", FieldWidgetKind.Input) { Format = "%.3f" });

        //
        TranslationField =
            Register(new FloatField<Float3Value>("Local Position", FieldWidgetKind.Input) { Format = "%.3f" });

        StartAreaField = Register(new FloatField<Float3Value>("Start Area", FieldWidgetKind.Input) { Format = "%.3f" });

        DirectionField = Register(new FloatField<Float3Value>("Direction", FieldWidgetKind.Input) { Format = "%.3f" });

        SpreadField = Register(new FloatField<Float1Value>("Spread", FieldWidgetKind.Input) { Format = "%.3f" });

        CreateSegment("Definition",
        [
            StartColorField, EndColorField, GravityField, DragField, SpeedMinMaxField, LifeMinMaxField,
            SizeStartEndField
        ]);
        CreateSegment("State", [TranslationField, StartAreaField, DirectionField, SpreadField]);
    }

    public override void Bind(ParticleInstance target)
    {
        ParticleCountField.Bind(
            () => target.Emitter.ParticleCount,
            value => target.Emitter.SetCount(int.Clamp((int)value, ParticleEmitter.MinCount, ParticleEmitter.MaxCount))
        );

        StartColorField.Bind(
            () => target.Emitter.GetDefinition().StartColor,
            value => target.Emitter.GetDefinition().StartColor = (Color4)value
        );

        EndColorField.Bind(
            () => target.Emitter.GetDefinition().EndColor,
            value => target.Emitter.GetDefinition().EndColor = (Color4)value
        );

        SizeStartEndField.Bind(
            () => target.Emitter.GetDefinition().SizeStartEnd,
            value => target.Emitter.GetDefinition().SizeStartEnd = (Vector2)value
        );

        GravityField.Bind(
            () => target.Emitter.GetDefinition().Gravity,
            value => target.Emitter.GetDefinition().Gravity = (Vector3)value
        );

        DragField.Bind(
            () => target.Emitter.GetDefinition().Drag,
            value => target.Emitter.GetDefinition().Drag = (float)value
        );

        SpeedMinMaxField.Bind(
            () => target.Emitter.GetDefinition().SpeedMinMax,
            value => target.Emitter.GetDefinition().SpeedMinMax = (Vector2)value
        );

        LifeMinMaxField.Bind(
            () => target.Emitter.GetDefinition().LifeMinMax,
            value => target.Emitter.GetDefinition().LifeMinMax = (Vector2)value
        );

        TranslationField.Bind(
            () => target.Emitter.GetState().Translation,
            value => target.Emitter.GetState().Translation = (Vector3)value
        );
        StartAreaField.Bind(
            () => target.Emitter.GetState().StartArea,
            value => target.Emitter.GetState().StartArea = (Vector3)value
        );
        DirectionField.Bind(
            () => target.Emitter.GetState().Direction,
            value => target.Emitter.GetState().Direction = (Vector3)value
        );
        SpreadField.Bind(
            () => target.Emitter.GetState().Spread,
            value => target.Emitter.GetState().Spread = (float)value
        );
    }
}