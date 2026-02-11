using ConcreteEngine.Core.Common.Numerics;
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