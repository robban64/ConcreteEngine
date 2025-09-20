using System.Numerics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace ConcreteEngine.Graphics.Primitives;

[StructLayout(LayoutKind.Sequential, Pack = 4)]
public readonly struct Matrix3(in Matrix4x4 m)
{
    public readonly float M11 = m.M11, M12 = m.M12, M13 = m.M13;
    public readonly float M21 = m.M21, M22 = m.M22, M23 = m.M23;
    public readonly float M31 = m.M31, M32 = m.M32, M33 = m.M33;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void CopyTo(Span<float> span)
    {
        span[0] = M11;
        span[1] = M12;
        span[2] = M13;
        span[3] = M21;
        span[4] = M22;
        span[5] = M23;
        span[6] = M31;
        span[7] = M32;
        span[8] = M33;
    }
}

[StructLayout(LayoutKind.Sequential)]
public readonly struct Mat3Std140(in Matrix3 m)
{
    public readonly Vector4 C0 = new(m.M11, m.M21, m.M31, 0f); // xyz = col0, w pad
    public readonly Vector4 C1 = new(m.M12, m.M22, m.M32, 0f); // xyz = col1, w pad
    public readonly Vector4 C2 = new(m.M13, m.M23, m.M33, 0f); // xyz = col2, w pad

    public Mat3Std140(in Matrix4x4 m) : this(new Matrix3(m))
    {
    }
}