using System.Numerics;
using ConcreteEngine.Core.Common.Numerics;
using ConcreteEngine.Core.Engine.Editor;

namespace ConcreteEngine.Core.Engine.Graphics.Particles;

public sealed class ParticleEmitterState(in EmitterParams emitterParams, in ParticleParams particleParams)
{
    public bool HasDirtyVisual { get; internal set; } = true;

    [InputColor]
    [Segment("Visual")]
    public ColorRgba StartColor { get; set => field = Set(field, value); } = particleParams.StartColor;

    [InputColor]
    [Segment("Visual")]
    public ColorRgba EndColor { get; set => field = Set(field, value); } = particleParams.EndColor;

    [InputNumber]
    [Segment("Visual")]
    public Vector2 SizeStartEnd { get; set => field = Set(field, value); } = particleParams.SizeStartEnd;

    //
    [InputNumber]
    [Segment("Simulation")]
    public float Spread { get; set; } = emitterParams.Spread;

    [InputNumber]
    [Segment("Simulation")]
    public Vector3 Gravity { get; set; } = new(0.0f, 0.015f, 0.0f);

    [InputNumber]
    [Segment("Simulation")]
    public Vector3 Direction { get; set; } = emitterParams.Direction;

    [InputNumber]
    [Segment("Simulation")]
    public Vector2 SpeedMinMax { get; set; } = emitterParams.SpeedMinMax;

    [InputNumber]
    [Segment("Simulation")]
    public Vector2 LifeMinMax { get; set; } = emitterParams.LifeMinMax;

    private T Set<T>(T field, T value) where T : unmanaged
    {
        if (EqualityComparer<T>.Default.Equals(field, value)) return field;
        HasDirtyVisual = true;
        return value;
    }
}