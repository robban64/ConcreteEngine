#region

using System.Numerics;
using System.Runtime.InteropServices;
using ConcreteEngine.Graphics.Gfx.Resources;

#endregion

namespace ConcreteEngine.Engine.Worlds.Data;

[StructLayout(LayoutKind.Sequential)]
internal struct ParticleStateData
{
    public Vector3 Position;
    public Vector3 PrevPosition;
    public Vector3 OriginalSpawnPos;
    public Vector3 Velocity;
    public float Life;
    public float MaxLife;
}

[StructLayout(LayoutKind.Sequential)]
public struct ParticleEmitterState
{
    public Vector3 LastSampleTranslation;
    public Vector3 Translation;
    public Vector3 StartArea;
    public Vector3 Direction;
    public float Spread;
}

[StructLayout(LayoutKind.Sequential)]
public struct ParticleDefinition
{
    public Vector4 StartColor;
    public Vector4 EndColor;

    public Vector3 Gravity;

    public Vector2 SpeedMinMax;
    public Vector2 SizeStartEnd;
    public Vector2 LifeMinMax;

    public static ParticleDefinition MakeDefault() => new()
    {
        StartColor = new Vector4(1.0f, 0.9f, 0.7f, 0.6f),
        EndColor = new Vector4(1.0f, 0.9f, 0.6f, 0.05f),
        Gravity = new Vector3(0.001f, -0.2f, 0.001f),
        LifeMinMax = new Vector2(6f, 10f),
        SizeStartEnd = new Vector2(0.05f, 0.18f),
        SpeedMinMax = new Vector2(0.02f, 0.11f),
    };
}