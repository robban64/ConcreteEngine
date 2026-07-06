using System.Numerics;
using ConcreteEngine.Core.Common.Numerics;
using ConcreteEngine.Core.Engine;
using ConcreteEngine.Core.Engine.Editor;
using ConcreteEngine.Editor.Lib;
using ConcreteEngine.Editor.Lib.Field;
using ConcreteEngine.Editor.Lib.Inspection;

namespace ConcreteEngine.Editor.Core.Provider.Impl;

internal sealed class InspectCameraFields : InspectorFields<EditorCamera>
{
    public readonly FloatField<Float3> Translation;
    public readonly FloatField<Float2> Orientation;
    public readonly FloatField<Float2> NearFar;
    public readonly FloatField<Float1> Fov;

    private static Camera Camera => CameraManager.Instance.Camera;

    public InspectCameraFields() : base(segmentCount: 2)
    {
        Translation = Register(new FloatField<Float3>("Translation", InputFieldKind.Input,
            static () => Camera.Translation,
            static value => Camera.Translation = (Vector3)value) { Format = "%.3f", Delay = FieldGetDelay.Low });

        Orientation = Register(new FloatField<Float2>("Orientation", InputFieldKind.Input,
            static () => (Vector2)Camera.Orientation,
            static value => Camera.Orientation = new YawPitch(value.X, value.Y))
        {
            Format = "%.3f", Delay = FieldGetDelay.Low
        });

        NearFar = Register(new FloatField<Float2>("Near/Far", InputFieldKind.Input,
            static () => (Float2)Camera.NearFarPlane,
            static (v) => Camera.NearFarPlane = (Vector2)v
        ) { Format = "%.2f", Delay = FieldGetDelay.High });

        Fov = Register(new FloatField<Float1>("Field of view", InputFieldKind.Slider,
            static () => Camera.Fov,
            static value => Camera.Fov = value.X)
        {
            Format = "%.2f",
            Delay = FieldGetDelay.High,
            LabelPlacement = FieldLabelPlacement.Top,
            Min = 10f,
            Max = 179f
        });

        CreateSegment("Transform", [Translation, Orientation]);
        CreateSegment("Projection", [NearFar, Fov]);
    }


    public override void Bind(EditorCamera target) { }
}