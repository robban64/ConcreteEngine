using System.Numerics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace ConcreteEngine.Shared.World;


[StructLayout(LayoutKind.Sequential)]
public struct ParticleEmitterState
{
    public Vector3 Translation;
    public Vector3 StartArea;
    public float Spread;
    public Vector3 Direction;
    public uint Seed;
    
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public uint NextSeed() => Seed++;
}

[StructLayout(LayoutKind.Sequential)]
public struct ParticleDefinition
{
    // Visuals
    public Vector4 StartColor;
    public Vector4 EndColor;
    public Vector2 SizeStartEnd;

    // Physics
    public Vector3 Gravity;     
    public float Drag;          
    
    // Spawn Parameters
    public Vector2 SpeedMinMax;
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

