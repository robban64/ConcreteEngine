using System.Numerics;
using System.Runtime.InteropServices;

namespace ConcreteEngine.Graphics;


[StructLayout(LayoutKind.Sequential, Pack = 4)]
public readonly struct Matrix3
{
    public readonly float M11, M12, M13;
    public readonly float M21, M22, M23;
    public readonly float M31, M32, M33;

    public Matrix3(in Matrix4x4 m)
    {
        M11 = m.M11; M12 = m.M12; M13 = m.M13;
        M21 = m.M21; M22 = m.M22; M23 = m.M23;
        M31 = m.M31; M32 = m.M32; M33 = m.M33;
    }

    public void CopyTo(Span<float> span)
    {
        span[0] = M11; span[1] = M12; span[2] = M13;
        span[3] = M21; span[4] = M22; span[5] = M23;
        span[6] = M31; span[7] = M32; span[8] = M33;
    }
}
