using System.Numerics;
using ConcreteEngine.Core.Engine.Editor;

namespace ConcreteEngine.Core.Engine.Graphics.Visuals;

[Inspect]
public sealed class EnvironmentSettings
{
    [InspectInclude]
    public AmbientSettings Ambient { get; } = new();

    [InspectInclude]
    public FogSettings FogSettings { get; } = new();

    public bool Commit()
    {
        var anyDirty = false;
        anyDirty |= Ambient.Commit();
        anyDirty |= FogSettings.Commit();
        return anyDirty;
    }
}

public sealed class AmbientSettings : VisualSettings
{
    [InputColor(HasAlpha = false)]
    public Vector3 Ambient
    {
        get;
        set => field = Set(field, value);
    } = new(0.34f, 0.38f, 0.44f);

    [InputColor(HasAlpha = false)]
    public Vector3 AmbientGround
    {
        get;
        set => field = Set(field, value);
    } = new(0.20f, 0.17f, 0.15f);

    [InputNumber(InputStyle.Drag, Label = "Exposure", Format = "%.3f", Speed = 0.01f, Min = 0f, Max = 2f)]
    public float Exposure
    {
        get;
        set => field = Set(field, value);
    } = 0.26f;
}