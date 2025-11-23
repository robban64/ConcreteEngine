#region

using System.Numerics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

#endregion

namespace ConcreteEngine.Common.Numerics;

[StructLayout(LayoutKind.Sequential)]
public readonly struct Matrix3
{
    public readonly float M11, M12, M13;
    public readonly float M21, M22, M23;
    public readonly float M31, M32, M33;

    public static Matrix3 Identity => new(1, 0, 0, 0, 1, 0, 0, 0, 1);


    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public Matrix3(in Matrix4x4 m)
    {
        M11 = m.M11;
        M12 = m.M12;
        M13 = m.M13;
        M21 = m.M21;
        M22 = m.M22;
        M23 = m.M23;
        M31 = m.M31;
        M32 = m.M32;
        M33 = m.M33;
    }

    public Matrix3(float m11, float m12, float m13,
        float m21, float m22, float m23,
        float m31, float m32, float m33)
    {
        M11 = m11;
        M12 = m12;
        M13 = m13;
        M21 = m21;
        M22 = m22;
        M23 = m23;
        M31 = m31;
        M32 = m32;
        M33 = m33;
    }

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