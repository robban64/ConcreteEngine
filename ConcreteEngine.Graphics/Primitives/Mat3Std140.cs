#region

using System.Numerics;
using System.Runtime.InteropServices;
using ConcreteEngine.Common.Numerics;

#endregion

namespace ConcreteEngine.Graphics.Primitives;

[StructLayout(LayoutKind.Sequential)]
public readonly struct Mat3Std140(in Matrix3 m)
{
    public readonly Vector4 C0 = new(m.M11, m.M21, m.M31, 0f); // w pad
    public readonly Vector4 C1 = new(m.M12, m.M22, m.M32, 0f); // w pad
    public readonly Vector4 C2 = new(m.M13, m.M23, m.M33, 0f); // w pad

    public Mat3Std140(in Matrix4x4 m) : this(new Matrix3(m))
    {
    }
}