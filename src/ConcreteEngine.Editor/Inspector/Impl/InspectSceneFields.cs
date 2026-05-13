using System.Numerics;
using ConcreteEngine.Core.Common.Numerics;
using ConcreteEngine.Core.Common.Numerics.Maths;
using ConcreteEngine.Core.Engine.Graphics;
using ConcreteEngine.Core.Engine.Scene;
using ConcreteEngine.Editor.Lib.Field;

namespace ConcreteEngine.Editor.Inspector.Impl;

internal sealed class InspectSceneFields : InspectorFields<InspectSceneObject>
{
    public readonly FloatField<Float3> TranslationField;
    public readonly FloatField<Float3> ScaleField;
    public readonly FloatField<Float3> RotationField;

    protected override FieldLayout DefaultLayout => FieldLayout.Top;
    protected override FieldGetDelay DefaultDelay => FieldGetDelay.Low;

    public InspectSceneFields() : base(segmentCount: 1)
    {
        TranslationField =
            Register(new FloatField<Float3>("Translation", FieldWidgetKind.Input) { Format = "%.3f" });
        ScaleField = Register(new FloatField<Float3>("Scale", FieldWidgetKind.Input) { Format = "%.3f" });
        RotationField = Register(new FloatField<Float3>("Rotation", FieldWidgetKind.Input) { Format = "%.3f" });

        CreateSegment("Transform", true, 0, [TranslationField, ScaleField, RotationField]);
    }

    public override void Bind(InspectSceneObject target)
    {
        TranslationField.Bind(
            () => target.Transform.Translation,
            value => target.Transform.Translation = (Vector3)value
        );
        ScaleField.Bind(
            () => target.Transform.Scale,
            value => target.Transform.Scale = (Vector3)value
        );
        RotationField.Bind(
            () => RotationMath.QuaternionToEulerDegrees(target.Transform.Rotation),
            value => target.Transform.Rotation = RotationMath.EulerDegreesToQuaternion((Vector3)value)
        );
    }
}

internal sealed class InspectModelInstanceFields : InspectorFields<ModelInstance>
{
    public readonly FloatField<Float3> TranslationField;
    public readonly FloatField<Float3> ScaleField;
    public readonly FloatField<Float3> RotationField;
    public readonly FloatField<Float3> LocalBoundsMinField;
    public readonly FloatField<Float3> LocalBoundsMaxField;

    protected override FieldLayout DefaultLayout => FieldLayout.Top;
    protected override FieldGetDelay DefaultDelay => FieldGetDelay.Low;

    public InspectModelInstanceFields() : base(segmentCount: 2)
    {
        TranslationField =
            Register(new FloatField<Float3>("Translation", FieldWidgetKind.Input) { Format = "%.3f" });
        ScaleField = Register(new FloatField<Float3>("Scale", FieldWidgetKind.Input) { Format = "%.3f" });
        RotationField = Register(new FloatField<Float3>("Rotation", FieldWidgetKind.Input) { Format = "%.3f" });
        LocalBoundsMinField = Register(new FloatField<Float3>("Min", FieldWidgetKind.Input) { Format = "%.3f" });
        LocalBoundsMaxField = Register(new FloatField<Float3>("Max", FieldWidgetKind.Input) { Format = "%.3f" });

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
    public readonly IntField<Int1> ParticleCountField;
    public readonly ColorField StartColorField;
    public readonly ColorField EndColorField;
    public readonly FloatField<Float2> SizeStartEndField;
    public readonly FloatField<Float3> GravityField;
    public readonly FloatField<Float1> DragField;
    public readonly FloatField<Float2> SpeedMinMaxField;
    public readonly FloatField<Float2> LifeMinMaxField;

    // State
    public readonly FloatField<Float3> TranslationField;
    public readonly FloatField<Float3> DirectionField;
    public readonly FloatField<Float1> SpreadField;

    protected override FieldLayout DefaultLayout => FieldLayout.Top;
    protected override FieldGetDelay DefaultDelay => FieldGetDelay.Low;


    public InspectParticleFields() : base(segmentCount: 2)
    {
        ParticleCountField = Register(new IntField<Int1>("Particle Count", FieldWidgetKind.Input)
            .WithProperties(FieldGetDelay.Medium, FieldLayout.Top, FieldTrigger.AfterChangeDeactive));

        StartColorField = Register(new ColorField("Start Color", true));

        EndColorField = Register(new ColorField("End Color", true));

        SizeStartEndField =
            Register(new FloatField<Float2>("Size Start / End", FieldWidgetKind.Input) { Format = "%.3f" });

        GravityField = Register(new FloatField<Float3>("Gravity", FieldWidgetKind.Input) { Format = "%.3f" });

        DragField = Register(new FloatField<Float1>("Drag", FieldWidgetKind.Input) { Format = "%.3f" });

        SpeedMinMaxField =
            Register(new FloatField<Float2>("Speed Min / Max", FieldWidgetKind.Input) { Format = "%.3f" });

        LifeMinMaxField =
            Register(new FloatField<Float2>("Life Min / Max", FieldWidgetKind.Input) { Format = "%.3f" });

        SpreadField = Register(new FloatField<Float1>("Spread", FieldWidgetKind.Input) { Format = "%.3f" });

        //
        TranslationField =
            Register(new FloatField<Float3>("Local Position", FieldWidgetKind.Input) { Format = "%.3f" });

        DirectionField = Register(new FloatField<Float3>("Direction", FieldWidgetKind.Input) { Format = "%.3f" });


        CreateSegment("State", [TranslationField, DirectionField]);

        CreateSegment("Definition",
        [
            StartColorField, EndColorField, GravityField, DragField, SpeedMinMaxField, LifeMinMaxField,
            SizeStartEndField, SpreadField
        ]);
    }

    public override void Bind(ParticleInstance target)
    {
        ParticleCountField.Bind(
            () => target.Emitter.ParticleCount,
            value => target.Emitter.SetCount(int.Clamp((int)value, ParticleEmitter.MinCount, ParticleEmitter.MaxCount))
        );

        StartColorField.Bind(
            () => target.Emitter.VisualParams().StartColor,
            value => target.Emitter.VisualParams().StartColor = (Color4)value
        );

        EndColorField.Bind(
            () => target.Emitter.VisualParams().EndColor,
            value => target.Emitter.VisualParams().EndColor = (Color4)value
        );

        SizeStartEndField.Bind(
            () => target.Emitter.VisualParams().SizeStartEnd,
            value => target.Emitter.VisualParams().SizeStartEnd = (Vector2)value
        );

        GravityField.Bind(
            () => target.Emitter.SpatialParams().Gravity,
            value => target.Emitter.SpatialParams().Gravity = (Vector3)value
        );

        DragField.Bind(
            () => target.Emitter.SpatialParams().Drag,
            value => target.Emitter.SpatialParams().Drag = (float)value
        );

        SpeedMinMaxField.Bind(
            () => target.Emitter.SpatialParams().SpeedMinMax,
            value => target.Emitter.SpatialParams().SpeedMinMax = (Vector2)value
        );

        LifeMinMaxField.Bind(
            () => target.Emitter.SpatialParams().LifeMinMax,
            value => target.Emitter.SpatialParams().LifeMinMax = (Vector2)value
        );
        
        SpreadField.Bind(
            () => target.Emitter.SpatialParams().Spread,
            value => target.Emitter.SpatialParams().Spread = (float)value
        );

        TranslationField.Bind(
            () => target.Emitter.Translation,
            value => target.Emitter.Translation = (Vector3)value
        );
        DirectionField.Bind(
            () => target.Emitter.Direction,
            value => target.Emitter.Direction = (Vector3)value
        );
    }
}