using System.Numerics;
using ConcreteEngine.Core.Common.Numerics;
using ConcreteEngine.Core.Renderer.Visuals;
using ConcreteEngine.Editor.Lib;

namespace ConcreteEngine.Editor.Controller.Proxy;

public sealed class EditorCameraProperties(
    ValueTextField<Size2D> viewport,
    FloatInputValueField<Float3Value> translation,
    FloatInputValueField<Float2Value> orientation,
    FloatInputValueField<Float2Value> nearFar,
    FloatSliderField<Float1Value> fov)
{
    public readonly ValueTextField<Size2D> Viewport = viewport;
    public readonly FloatInputValueField<Float3Value> Translation = translation;
    public readonly FloatInputValueField<Float2Value> Orientation = orientation;
    public readonly FloatInputValueField<Float2Value> NearFar = nearFar;
    public readonly FloatSliderField<Float1Value> Fov = fov;
}

public abstract class EditorLightController
{
    public abstract Size2D ScreenFboSize { get; }
    public abstract AmbientParams Ambient { get; set; }
    public abstract FogParams Fog { get; set; }
    public abstract SunLightParams SunLight { get; set; }
    public abstract ShadowParams Shadow { get; set; }
}

public abstract class EditorCameraController
{
    public abstract Size2D Viewport { get; }

    public abstract Vector3 Translation { get; set; }
    public abstract YawPitch Orientation { get; set; }
    public abstract float Fov { get; set; }
    public abstract float FarPlane { get; set; }
    public abstract float NearPlane { get; set; }
}