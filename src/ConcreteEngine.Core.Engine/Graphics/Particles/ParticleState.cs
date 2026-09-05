using System.Numerics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using ConcreteEngine.Core.Common.Numerics;

namespace ConcreteEngine.Core.Engine.Graphics.Particles;

[StructLayout(LayoutKind.Sequential)]
public struct ParticleLifeState(float life, float lifeInvMax)
{
    public float Life = life;
    public float LifeInvMax = lifeInvMax;
}

[StructLayout(LayoutKind.Sequential)]
public readonly struct ParticleLut(float size, ColorRgba color)
{
    public readonly float Size = size;
    public readonly ColorRgba Color = color;
}