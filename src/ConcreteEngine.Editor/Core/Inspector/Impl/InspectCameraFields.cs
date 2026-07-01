using System.Numerics;
using ConcreteEngine.Core.Common.Numerics;
using ConcreteEngine.Core.Engine;
using ConcreteEngine.Editor.Lib.Field;

namespace ConcreteEngine.Editor.Core.Inspector.Impl;

internal sealed class InspectCameraFields : InspectorFields<EditorCamera>
{
    public readonly FloatField<Float3> Translation;
    public readonly FloatField<Float2> Orientation;
    public readonly FloatField<Float2> NearFar;
    public readonly FloatField<Float1> Fov;

    private static Camera Camera => CameraManager.Instance.Camera;

    public InspectCameraFields() : base(segmentCount: 2)
    {
        Translation = Register(new FloatField<Float3>("Translation", FieldWidgetKind.Input,
            static () => Camera.Translation,
            static value => Camera.Translation = (Vector3)value) { Format = "%.3f", Delay = FieldGetDelay.Low });

        Orientation = Register(new FloatField<Float2>("Orientation", FieldWidgetKind.Input,
            static () => (Vector2)Camera.Orientation,
            static value => Camera.Orientation = new YawPitch(value.X, value.Y))
        {
            Format = "%.3f", Delay = FieldGetDelay.Low
        });

        NearFar = Register(new FloatField<Float2>("Near/Far", FieldWidgetKind.Input,
            static () => new Float2(Camera.NearPlane, Camera.FarPlane),
            static value =>
            {
                Camera.NearPlane = value.X;
                Camera.FarPlane = value.Y;
            }) { Format = "%.2f", Delay = FieldGetDelay.High });

        Fov = Register(new FloatField<Float1>("Field of view", FieldWidgetKind.Slider,
            static () => Camera.Fov,
            static value => Camera.Fov = value.X)
        {
            Format = "%.2f",
            Delay = FieldGetDelay.High,
            Layout = FieldLayout.Top,
            Min = 10f,
            Max = 179f
        });

        CreateSegment("Transform", [Translation, Orientation]);
        CreateSegment("Projection", [NearFar, Fov]);
    }


    public override void Bind(EditorCamera target) { }
}