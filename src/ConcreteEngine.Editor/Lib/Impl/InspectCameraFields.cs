using System.Numerics;
using ConcreteEngine.Core.Common.Numerics;
using ConcreteEngine.Editor.Core;
using ConcreteEngine.Editor.Lib.Field;
using static ConcreteEngine.Editor.EngineObjectStore;

namespace ConcreteEngine.Editor.Lib.Impl;

internal sealed class InspectCameraFields : InspectorFields<EditorCamera>
{
    public readonly FloatField<Float3Value> Translation;
    public readonly FloatField<Float2Value> Orientation;
    public readonly FloatField<Float2Value> NearFar;
    public readonly FloatField<Float1Value> Fov;

    public InspectCameraFields() : base(segmentCount: 2)
    {
        Translation = Register(new FloatField<Float3Value>("Translation", FieldWidgetKind.Input,
            static () => Camera.Translation,
            static value => Camera.Translation = (Vector3)value) { Format = "%.3f" });

        Orientation = Register(new FloatField<Float2Value>("Orientation", FieldWidgetKind.Input,
            static () => (Vector2)Camera.Orientation,
            static value => Camera.Orientation = new YawPitch(value.X, value.Y)) { Format = "%.3f" });

        NearFar = Register(new FloatField<Float2Value>("Near/Far", FieldWidgetKind.Input,
            static () => new Float2Value(Camera.NearPlane, Camera.FarPlane),
            static value =>
            {
                Camera.NearPlane = value.X;
                Camera.FarPlane = value.Y;
            }) { Format = "%.2f", Delay = FieldGetDelay.High });

        Fov = Register(new FloatField<Float1Value>("Field of view", FieldWidgetKind.Slider,
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