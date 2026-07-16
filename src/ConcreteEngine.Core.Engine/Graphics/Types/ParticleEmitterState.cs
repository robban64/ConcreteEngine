using System.Numerics;
using System.Runtime.InteropServices;
using ConcreteEngine.Core.Common.Numerics;
using ConcreteEngine.Core.Engine.Editor;

namespace ConcreteEngine.Core.Engine.Graphics;

[StructLayout(LayoutKind.Sequential)]
public struct ParticleCpuInstance
{
    public Vector3 Position;
    public Vector3 Velocity;
    public float Life;
    public float MaxLife;
}

[StructLayout(LayoutKind.Sequential)]
public struct ParticleParams
{
    [InputColor] public Color4 StartColor;
    [InputColor] public Color4 EndColor;
    [InputNumber] public Vector2 SizeStartEnd;

    public static ParticleParams MakeDefault() =>
        new()
        {
            StartColor = new Color4(1.0f, 0.9f, 0.7f, 0.6f),
            EndColor = new Color4(1.0f, 0.9f, 0.6f, 0.05f),
            SizeStartEnd = new Vector2(0.12f, 0.22f),
        };
}

[StructLayout(LayoutKind.Sequential)]
public struct EmitterParams
{
    // Physics
    [InputNumber] public Vector3 Gravity;
    [InputNumber] public float Drag;
    [InputNumber] public float Spread;

    // Spawn Parameters
    [InputNumber] public Vector3 Direction;
    [InputNumber] public Vector2 SpeedMinMax;
    [InputNumber] public Vector2 LifeMinMax;

    public static EmitterParams MakeDefault() =>
        new()
        {
            Spread = 3.14f,
            Gravity = new Vector3(0.0f, 0.015f, 0.0f),
            LifeMinMax = new Vector2(6f, 10f),
            SpeedMinMax = new Vector2(0.02f, 0.05f)
        };
}