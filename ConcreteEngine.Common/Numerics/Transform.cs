using System.Numerics;
using System.Runtime.InteropServices;

namespace ConcreteEngine.Common.Numerics;

[StructLayout(LayoutKind.Sequential)]
public struct Transform(in Vector3 translation, in Vector3 scale, in Quaternion rotation)
{
    public Vector3 Translation = translation;
    public Quaternion Rotation = rotation;
    public Vector3 Scale = scale;
}

