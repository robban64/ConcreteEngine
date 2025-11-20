using System.Numerics;
using System.Runtime.InteropServices;

namespace ConcreteEngine.Engine.Worlds.Data;

[StructLayout(LayoutKind.Sequential)]
internal struct ParticleInstanceData
{
    public Vector4 Position;
    public Vector4 Color;
}

[StructLayout(LayoutKind.Sequential)]
internal struct ParticleStateData
{
    public Vector3 Position;
    public Vector3 PrevPosition;
    public Vector3 Velocity;
    public float Life;
    public float MaxLife;
}