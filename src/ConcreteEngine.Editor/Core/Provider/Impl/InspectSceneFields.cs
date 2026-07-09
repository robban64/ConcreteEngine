using System.Numerics;
using ConcreteEngine.Core.Common.Numerics;
using ConcreteEngine.Core.Common.Numerics.Maths;
using ConcreteEngine.Core.Engine.Editor;
using ConcreteEngine.Core.Engine.Graphics;
using ConcreteEngine.Core.Engine.Scene;
using ConcreteEngine.Editor.Lib.Field;
using ConcreteEngine.Editor.Lib.Inspection;
using ConcreteEngine.Editor.Lib.Widgets;

namespace ConcreteEngine.Editor.Core.Provider.Impl;

internal sealed class InspectSceneFields : InspectorFields<InspectSceneObject>
{
    public readonly FloatField<Float3> TranslationField;
    public readonly FloatField<Float3> ScaleField;
    public readonly FloatField<Float3> RotationField;

    protected override LabelPlacement DefaultLabelPlacement => LabelPlacement.Top;
    protected override FieldFetchDelay DefaultDelay => FieldFetchDelay.Low;

    public InspectSceneFields() : base(segmentCount: 1)
    {
        TranslationField =
            Register(new FloatField<Float3>("Translation", InputFieldKind.Input) { Format = "%.3f" });
        ScaleField = Register(new FloatField<Float3>("Scale", InputFieldKind.Input) { Format = "%.3f" });
        RotationField = Register(new FloatField<Float3>("Rotation", InputFieldKind.Input) { Format = "%.3f" });

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

/*
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
*/