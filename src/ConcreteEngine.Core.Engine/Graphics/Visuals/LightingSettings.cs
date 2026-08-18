using System.Numerics;
using ConcreteEngine.Core.Common.Numerics.Maths;
using ConcreteEngine.Core.Engine.Editor;

namespace ConcreteEngine.Core.Engine.Graphics.Visuals;
// @formatter:off


[Inspect]
public sealed class LightingSettings : VisualStateObject
{
    [InputNumber(Format = "%.3f", Speed = 0.01f, Min = -1f, Max = 1f)]
    public Vector3 Direction
    {
        get;
        set
        {
            field = value;
            IsDirty = true;
        }
    } = new Vector3(-0.35f, -0.95f, 0.25f);

    [InputColor(HasAlpha = false)]
    public Vector3 Diffuse
    {
        get;
        set
        {
            field = value;
            IsDirty = true;
        }
    } = new Vector3(1.05f, 0.92f, 0.82f);

    [InputNumber(InputStyle.Drag, Format = "%.3f", Speed = 0.01f, Min = 0f, Max = 10f)]
    public float Intensity
    {
        get;
        set
        {
            field = value;
            IsDirty = true;
        }
    } = 1.35f;

    [InputNumber(InputStyle.Drag, Format = "%.3f", Speed = 0.01f, Min = 0f, Max = 10f)]
    public float Specular
    {
        get;
        set
        {
            field = value;
            IsDirty = true;
        }
    } = 0.75f;
    
    [InputColor(HasAlpha = false, Segment = "Ambient")]
    public Vector3 Ambient
    {
        get;
        set
        {
            field = value;
            IsDirty = true;
        }
    } = new Vector3(0.34f, 0.38f, 0.44f);

    [InputColor(HasAlpha = false, Segment = "Ambient")]
    public Vector3 AmbientGround
    {
        get;
        set
        {
            field = value;
            IsDirty = true;
        }
    } = new Vector3(0.20f, 0.17f, 0.15f);

    [InputNumber(InputStyle.Drag, Segment = "Ambient", Format = "%.3f", Speed = 0.01f, Min = 0f, Max = 2f)]
    public float Exposure
    {
        get;
        set
        {
            if(FloatMath.NearlyEqual(field, value)) return;
            field = value;
            IsDirty = true;
        }
    } = 0.26f;
}