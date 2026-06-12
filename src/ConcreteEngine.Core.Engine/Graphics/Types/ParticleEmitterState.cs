using System.Numerics;
using System.Runtime.InteropServices;
using ConcreteEngine.Core.Common.Numerics;

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
public struct EmitterVisualParams
{
    public Color4 StartColor;
    public Color4 EndColor;
    public Vector2 SizeStartEnd;

    public static EmitterVisualParams MakeDefault() =>
        new()
        {
            StartColor = new Color4(1.0f, 0.9f, 0.7f, 0.6f),
            EndColor = new Color4(1.0f, 0.9f, 0.6f, 0.05f),
            SizeStartEnd = new Vector2(0.12f, 0.22f),
        };
}

[StructLayout(LayoutKind.Sequential)]
public struct EmitterSpatialParams
{
    // Physics
    public Vector3 Gravity;
    public float Drag;
    public float Spread;

    // Spawn Parameters
    public Vector2 SpeedMinMax;
    public Vector2 LifeMinMax;

    public static EmitterSpatialParams MakeDefault() =>
        new()
        {
            Spread = 3.14f,
            Gravity = new Vector3(0.0f, 0.015f, 0.0f),
            LifeMinMax = new Vector2(6f, 10f),
            SpeedMinMax = new Vector2(0.02f, 0.05f)
        };
}