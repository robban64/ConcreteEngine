using System.Numerics;
using System.Runtime.InteropServices;

namespace ConcreteEngine.Core.Common.Numerics;

[StructLayout(LayoutKind.Sequential)]
public struct Matrix3
{
    public Vector3 V0;
    public Vector3 V1;
    public Vector3 V2;
}

[StructLayout(LayoutKind.Sequential)]
public struct Matrix3X4
{
    public Vector4 V0;
    public Vector4 V1;
    public Vector4 V2;
}