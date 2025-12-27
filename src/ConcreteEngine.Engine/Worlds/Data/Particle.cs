using System.Numerics;
using System.Runtime.InteropServices;

namespace ConcreteEngine.Engine.Worlds.Data;

[StructLayout(LayoutKind.Sequential)]
internal struct ParticleStateData
{
    public Vector3 Position;
    public float Life;
    public Vector3 Velocity;
    public float MaxLife;
}