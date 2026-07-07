using System.Numerics;
using ConcreteEngine.Core.Common.Numerics;
using ConcreteEngine.Core.Engine;
using ConcreteEngine.Core.Engine.Assets;
using ConcreteEngine.Core.Engine.Editor;
using ConcreteEngine.Editor.Lib;
using ConcreteEngine.Editor.Lib.Field;
using ConcreteEngine.Editor.Lib.Inspection;
using ConcreteEngine.Editor.Lib.Widgets;
using ConcreteEngine.Graphics.Gfx;

namespace ConcreteEngine.Editor.Core.Provider;

internal static class InspectorProvider
{
    private static Camera Camera => Inspector<Camera>.Target!;
    public static void RegisterCamera()
    {
        Inspector<Camera>.Bind(EditorCamera.Instance.Camera);
        Inspector<Camera>.Register(
            new FloatInput<Float3>(nameof(Camera.Translation), InputFieldKind.Input) { Format = "%.3f" },
            static () => (Float3)Camera.Translation,
            static (v) => Camera.Translation = (Vector3)v
        );

        Inspector<Camera>.Register(
            new FloatInput<Float2>(nameof(Camera.Orientation), InputFieldKind.Input) { Format = "%.3f" },
            static () => new Float2(Camera.Orientation.Yaw, Camera.Orientation.Pitch),
            static (v) => Camera.Orientation = new YawPitch(v.X, v.Y)
        );

        Inspector<Camera>.Register(
            new FloatInput<Float2>("Near/Far", InputFieldKind.Input),
            static () => (Float2)Camera.NearFarPlane,
            static (v) => Camera.NearFarPlane = (Vector2)v,
            FieldGetDelay.High
        );
        Inspector<Camera>.Register(
            new FloatInput<Float1>("Field of view", InputFieldKind.Slider) { Min = 10f, Max = 179f },
            static () => (Float1)Camera.Fov,
            static (v) => Camera.Fov = (float)v,
            FieldGetDelay.High
        );
    }
}