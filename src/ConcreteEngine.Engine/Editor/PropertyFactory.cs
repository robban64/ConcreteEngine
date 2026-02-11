using System.Numerics;
using ConcreteEngine.Core.Common.Numerics;
using ConcreteEngine.Core.Renderer.Material;
using ConcreteEngine.Editor.Controller.Proxy;
using ConcreteEngine.Editor.Lib;
using ConcreteEngine.Editor.Panels;
using ConcreteEngine.Engine.Assets;
using ConcreteEngine.Engine.Worlds;

namespace ConcreteEngine.Engine.Editor;

public sealed class CameraPropertyProvider(Camera camera)
{
    public EditorCameraProperties Generate()
    {
        var viewport =
            new ValueTextField<Size2D>("Dimensions", () => camera.Viewport, TextFieldFormatter.SizeAspectFormat);

        var translation = new FloatInputValueField<Float3Value>("Translation",
            () => camera.Translation,
            value => camera.Translation = (Vector3)value) { Format = "%.3f" };

        var orientation = new FloatInputValueField<Float2Value>("Orientation",
            () => (Vector2)camera.Orientation,
            value => camera.Orientation = new YawPitch(value.X, value.Y)) { Format = "%.3f" };

        var nearFar = new FloatInputValueField<Float2Value>("Near/Far",
            () => new Float2Value(camera.NearPlane, camera.FarPlane),
            value =>
            {
                camera.NearPlane = value.X;
                camera.FarPlane = value.Y;
            }) { Format = "%.2f" };

        var fov = new FloatSliderField<Float1Value>("Field of view",
            () => camera.Fov,
            value => camera.Fov = value.X) { Min = 10f, Max = 179f, Format = "%.2f" };

        nearFar.Delay = PropertyGetDelay.High;
        fov.Delay = PropertyGetDelay.High;
        viewport.Delay = PropertyGetDelay.VeryHigh;

        return new EditorCameraProperties(viewport, translation, orientation, nearFar, fov);
    }
}