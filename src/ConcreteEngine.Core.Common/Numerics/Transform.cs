using System.Numerics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace ConcreteEngine.Core.Common.Numerics;

[StructLayout(LayoutKind.Sequential)]
public struct Transform(in Vector3 translation, in Vector3 scale, in Quaternion rotation)
{
    public Transform(in Vector3 translation) : this(in translation, Vector3.One, Quaternion.Identity) { }

    public Vector3 Translation = translation;
    public Quaternion Rotation = rotation;
    public Vector3 Scale = scale;

    public static Transform Identity = new(Vector3.Zero, Vector3.One, Quaternion.Identity);

    public static void FromMatrix(in Matrix4x4 matrix, out Transform transform)
    {
        Matrix4x4.Decompose(matrix, out transform.Scale, out transform.Rotation, out transform.Translation);
    }
}