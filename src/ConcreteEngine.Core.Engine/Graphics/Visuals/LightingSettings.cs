using System.Numerics;
using ConcreteEngine.Core.Common.Numerics.Maths;
using ConcreteEngine.Core.Engine.Editor;

namespace ConcreteEngine.Core.Engine.Graphics.Visuals;

[Inspect]
public sealed class LightingSettings : VisualStateObject
{
    //
    [InputNumber(Format = "%.3f", Speed = 0.01f, Min = -1f, Max = 1f,Segment = "Sun")]
    public Vector3 Direction
    {
        get;
        set => field = Set(field, value, ref IsDirty);
    } = new(-0.35f, -0.95f, 0.25f);

    [InputColor(HasAlpha = false,Segment = "Sun")]
    public Vector3 Diffuse
    {
        get;
        set => field = Set(field, value, ref IsDirty);
    } = new(1.05f, 0.92f, 0.82f);

    [InputNumber(InputStyle.Drag, Format = "%.3f", Speed = 0.01f, Min = 0f, Max = 10f,Segment = "Sun")]
    public float Intensity
    {
        get;
        set => field = Set(field, value, ref IsDirty);
    } = 1.35f;

    //
    [InputNumber(InputStyle.Drag, Format = "%.3f", Speed = 0.01f, Min = 0f, Max = 10f,Segment = "Sun")]
    public float Specular
    {
        get;
        set => field = Set(field, value, ref IsDirty);
    } = 0.75f;

    [InputColor(HasAlpha = false, Segment = "Ambient")]
    public Vector3 Ambient
    {
        get;
        set => field = Set(field, value, ref IsDirty);
    } = new(0.34f, 0.38f, 0.44f);

    [InputColor(HasAlpha = false, Segment = "Ambient")]
    public Vector3 AmbientGround
    {
        get;
        set => field = Set(field, value, ref IsDirty);
    } = new(0.20f, 0.17f, 0.15f);

    [InputNumber(InputStyle.Drag, Segment = "Ambient", Label = "Exposure", Format = "%.3f", Speed = 0.01f, Min = 0f, Max = 2f)]
    public float Exposure
    {
        get;
        set => field = Set(field, value, ref IsDirty);
    } = 0.26f;
    
}