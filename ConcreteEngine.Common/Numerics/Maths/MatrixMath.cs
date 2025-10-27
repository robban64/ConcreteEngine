#region

using System.Numerics;
using System.Runtime.CompilerServices;

#endregion

// ReSharper disable InconsistentNaming

namespace ConcreteEngine.Common.Numerics.Maths;

public static class MatrixMath
{
    [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
    public static void CreateModelMatrix(in Vector3 transform, in Vector3 scale, in Quaternion rotation,
        out Matrix4x4 mat)
    {
        float x = rotation.X, y = rotation.Y, z = rotation.Z, w = rotation.W;
        float xx = x + x, yy = y + y, zz = z + z;
        float xy = x * yy, xz = x * zz, yz = y * zz;
        float wx = w * xx, wy = w * yy, wz = w * zz;
        float x2 = x * xx, y2 = y * yy, z2 = z * zz;

        float r11 = 1f - (y2 + z2), r22 = 1f - (x2 + z2), r33 = 1f - (x2 + y2);
        float r12 = xy + wz, r13 = xz - wy, r21 = xy - wz;
        float r23 = yz + wx, r31 = xz + wy, r32 = yz - wx;

        mat.M11 = r11 * scale.X;
        mat.M12 = r12 * scale.Y;
        mat.M13 = r13 * scale.Z;
        mat.M14 = 0f;

        mat.M21 = r21 * scale.X;
        mat.M22 = r22 * scale.Y;
        mat.M23 = r23 * scale.Z;
        mat.M24 = 0f;

        mat.M31 = r31 * scale.X;
        mat.M32 = r32 * scale.Y;
        mat.M33 = r33 * scale.Z;
        mat.M34 = 0f;

        mat.M41 = transform.X;
        mat.M42 = transform.Y;
        mat.M43 = transform.Z;
        mat.M44 = 1f;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
    public static void CreateModelMatrix(in Vector3 translation, float scale, in Quaternion rotation, out Matrix4x4 mat)
    {
        float x = rotation.X, y = rotation.Y, z = rotation.Z, w = rotation.W;
        float xx = x + x, yy = y + y, zz = z + z;
        float xy = x * yy, xz = x * zz, yz = y * zz;
        float wx = w * xx, wy = w * yy, wz = w * zz;
        float x2 = x * xx, y2 = y * yy, z2 = z * zz;

        mat.M11 = (1f - (y2 + z2)) * scale;
        mat.M12 = (xy + wz) * scale;
        mat.M13 = (xz - wy) * scale;
        mat.M14 = 0f;
        mat.M21 = (xy - wz) * scale;
        mat.M22 = (1f - (x2 + z2)) * scale;
        mat.M23 = (yz + wx) * scale;
        mat.M24 = 0f;
        mat.M31 = (xz + wy) * scale;
        mat.M32 = (yz - wx) * scale;
        mat.M33 = (1f - (x2 + y2)) * scale;
        mat.M34 = 0f;

        mat.M41 = translation.X;
        mat.M42 = translation.Y;
        mat.M43 = translation.Z;
        mat.M44 = 1f;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
    public static void CreateNormalMatrix(in Matrix4x4 m, out Matrix3 n)
    {
        float a = m.M11, b = m.M12, c = m.M13;
        float d = m.M21, e = m.M22, f = m.M23;
        float g = m.M31, h = m.M32, k = m.M33;

        float C11 = e * k - f * h;
        float C12 = -(d * k - f * g);
        float C13 = d * h - e * g;

        float C21 = -(b * k - c * h);
        float C22 = a * k - c * g;
        float C23 = -(a * h - b * g);

        float C31 = b * f - c * e;
        float C32 = -(a * f - c * d);
        float C33 = a * e - b * d;

        float det = a * C11 + b * C12 + c * C13;
        if (MathF.Abs(det) < 1e-8f)
        {
            n = Matrix3.Identity;
            return;
        }

        float s = 1f / det;
        n = new Matrix3(C11 * s, C12 * s, C13 * s, C21 * s, C22 * s, C23 * s, C31 * s, C32 * s, C33 * s);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
    public static bool InvertAffine(in Matrix4x4 m, out Matrix4x4 inv)
    {
        float a = m.M11, b = m.M12, c = m.M13;
        float d = m.M21, e = m.M22, f = m.M23;
        float g = m.M31, h = m.M32, k = m.M33;

        float C11 = e * k - f * h, C12 = -(d * k - f * g), C13 = d * h - e * g;
        float C21 = -(b * k - c * h), C22 = a * k - c * g, C23 = -(a * h - b * g);
        float C31 = b * f - c * e, C32 = -(a * f - c * d), C33 = a * e - b * d;

        float det = a * C11 + b * C12 + c * C13;
        if (MathF.Abs(det) < 1e-8f)
        {
            inv = Matrix4x4.Identity;
            return false;
        }

        float s = 1f / det;

        float i11 = C11 * s, i12 = C21 * s, i13 = C31 * s;
        float i21 = C12 * s, i22 = C22 * s, i23 = C32 * s;
        float i31 = C13 * s, i32 = C23 * s, i33 = C33 * s;

        float tx = m.M41, ty = m.M42, tz = m.M43;
        float itx = -(tx * i11 + ty * i12 + tz * i13);
        float ity = -(tx * i21 + ty * i22 + tz * i23);
        float itz = -(tx * i31 + ty * i32 + tz * i33);

        inv = new Matrix4x4(
            i11, i12, i13, 0f,
            i21, i22, i23, 0f,
            i31, i32, i33, 0f,
            itx, ity, itz, 1f);

        return true;
    }
}