using System.Numerics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using ConcreteEngine.Core.Common.Numerics;

namespace ConcreteEngine.Core.Engine.Graphics.Particles;

[StructLayout(LayoutKind.Sequential)]
public struct ParticleState
{
    public Vector4 Velocity;
    public Vector4 Position;
}

[StructLayout(LayoutKind.Sequential)]
public struct ParticleLifeState(float life, float lifeInvMax)
{
    public float Life = life;
    public float LifeInvMax = lifeInvMax;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool SimulateLife(float dt, out byte lutIndex)
    {
        var l = Life = -dt;
        if (l > 0)
        {
            var d = 1f - l * LifeInvMax;
            lutIndex = (byte)float.FusedMultiplyAdd(d, 255f, 0.5f);
            return true;
        }
        else
        {
            lutIndex = 0;
            return false;
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public readonly float LutIndex(float life)
    {
        var l = float.FusedMultiplyAdd(-life, LifeInvMax, 1f);
        return float.FusedMultiplyAdd(l, 255f, 0.5f);
    }
}

[StructLayout(LayoutKind.Sequential)]
public readonly struct ParticleLut(float size, ColorRgba color)
{
    public readonly float Size = size;
    public readonly ColorRgba Color = color;
}