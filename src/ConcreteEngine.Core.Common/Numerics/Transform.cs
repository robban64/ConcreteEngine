using System.Numerics;
using System.Runtime.InteropServices;
using Silk.NET.Maths;

namespace ConcreteEngine.Core.Common.Numerics;

[StructLayout(LayoutKind.Sequential)]
public struct Transform(in Vector3 translation, in Vector3 scale, in Quaternion rotation)
{
    public Vector3 Translation = translation;
    public Quaternion Rotation = rotation;
    public Vector3 Scale = scale;

    public static Transform Identity = new(Vector3.Zero, Vector3.One, Quaternion.Identity);
}