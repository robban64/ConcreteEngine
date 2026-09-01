using System.Numerics;
using System.Runtime.InteropServices;

namespace ConcreteEngine.Core.Common.Numerics;

[StructLayout(LayoutKind.Sequential)]
public struct Transform
{
    public Vector3 Translation;
    public Quaternion Rotation;
    public Vector3 Scale;

    public Transform( Vector3 translation)
    {
        Translation = translation;
        Rotation = Quaternion.Identity;
        Scale = Vector3.One;
    }

    public Transform( Vector3 translation,  Vector3 scale,  Quaternion rotation)
    {
        Translation = translation;
        Rotation = rotation;
        Scale = scale;
    }

    public static Transform Identity { get; } = new(Vector3.Zero, Vector3.One, Quaternion.Identity);

    public static void FromMatrix(in Matrix4x4 matrix, out Transform transform)
    {
        Matrix4x4.Decompose(matrix, out transform.Scale, out transform.Rotation, out transform.Translation);
    }
}