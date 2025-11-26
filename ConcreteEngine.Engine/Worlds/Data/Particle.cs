#region

using System.Numerics;
using System.Runtime.InteropServices;

#endregion

namespace ConcreteEngine.Engine.Worlds.Data;

[StructLayout(LayoutKind.Sequential)]
internal struct ParticleInstanceData
{
    public Vector4 PositionSize;
    public Vector4 Color;
}

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

public struct ParticleDefinition
{
    public Vector4 StartColor;
    public Vector4 EndColor;

    public Vector3 Gravity;

    public Vector2 SpeedMinMax;
    public Vector2 SizeStartEnd;
    public Vector2 LifeMinMax;
}