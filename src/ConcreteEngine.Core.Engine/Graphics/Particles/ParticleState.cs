using System.Numerics;
using System.Runtime.InteropServices;

namespace ConcreteEngine.Core.Engine.Graphics.Particles;

[StructLayout(LayoutKind.Sequential)]
public struct ParticleState
{
    public Vector3 Position;
    public Vector3 Velocity;
    public float Life;
    public float InvLife;
    public float InvMaxLife;
}