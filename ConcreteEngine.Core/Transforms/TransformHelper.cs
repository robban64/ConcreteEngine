#region

using System.Numerics;
using System.Runtime.CompilerServices;
using ConcreteEngine.Common.Numerics;

#endregion

namespace ConcreteEngine.Core;

public static class TransformHelper
{
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Matrix4x4 CreateTransform2D(Vector2 translation, Vector2 scale,
        float rotation)
    {
        var translationMat = Matrix4x4.CreateTranslation(translation.ToVec3());
        var rotationMat = Matrix4x4.CreateRotationZ(rotation);
        var scaleMat = Matrix4x4.CreateScale(scale.ToVec3(1));

        return scaleMat * rotationMat * translationMat;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Matrix4x4 CreateTransform(Vector3 translation, Vector3 scale, Quaternion rotation)
    {
        var translationMat = Matrix4x4.CreateTranslation(translation);
        var rotationMat = Matrix4x4.CreateFromQuaternion(rotation);
        var scaleMat = Matrix4x4.CreateScale(scale);
        return Matrix4x4.Identity * rotationMat * scaleMat * translationMat;
    }


    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void GetNormalMatrix(in Matrix4x4 matrix, out Matrix3 normal)
    {
        if (!Matrix4x4.Invert(matrix, out var inv)) inv = Matrix4x4.Identity;
        var invT = Matrix4x4.Transpose(inv);

        normal = new Matrix3(in invT);
    }
}