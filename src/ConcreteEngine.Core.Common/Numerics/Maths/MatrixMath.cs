using System.Numerics;
using System.Runtime.CompilerServices;

// ReSharper disable InconsistentNaming

namespace ConcreteEngine.Core.Common.Numerics.Maths;

public static class MatrixMath
{
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void MultiplyAffine(in Matrix4x4 a, in Matrix4x4 b, out Matrix4x4 r)
    {
        // 3x3
        float r11 = a.M11 * b.M11 + a.M12 * b.M21 + a.M13 * b.M31;
        float r12 = a.M11 * b.M12 + a.M12 * b.M22 + a.M13 * b.M32;
        float r13 = a.M11 * b.M13 + a.M12 * b.M23 + a.M13 * b.M33;

        // Row 2
        float r21 = a.M21 * b.M11 + a.M22 * b.M21 + a.M23 * b.M31;
        float r22 = a.M21 * b.M12 + a.M22 * b.M22 + a.M23 * b.M32;
        float r23 = a.M21 * b.M13 + a.M22 * b.M23 + a.M23 * b.M33;

        // Row 3
        float r31 = a.M31 * b.M11 + a.M32 * b.M21 + a.M33 * b.M31;
        float r32 = a.M31 * b.M12 + a.M32 * b.M22 + a.M33 * b.M32;
        float r33 = a.M31 * b.M13 + a.M32 * b.M23 + a.M33 * b.M33;

        // row-Major: (Translation A * Rotation B) + Translation B
        // A.M4x as a row vector and multiply by the 3x3 part of B
        float tx = a.M41 * b.M11 + a.M42 * b.M21 + a.M43 * b.M31 + b.M41;
        float ty = a.M41 * b.M12 + a.M42 * b.M22 + a.M43 * b.M32 + b.M42;
        float tz = a.M41 * b.M13 + a.M42 * b.M23 + a.M43 * b.M33 + b.M43;

        r = new Matrix4x4(
            r11, r12, r13, 0f,
            r21, r22, r23, 0f,
            r31, r32, r33, 0f,
            tx, ty, tz, 1f);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void WriteMultiplyAffine(scoped ref Matrix4x4 dest, in Matrix4x4 a, in Matrix4x4 b)
    {
        dest.M11 = a.M11 * b.M11 + a.M12 * b.M21 + a.M13 * b.M31;
        dest.M12 = a.M11 * b.M12 + a.M12 * b.M22 + a.M13 * b.M32;
        dest.M13 = a.M11 * b.M13 + a.M12 * b.M23 + a.M13 * b.M33;
        dest.M14 = 0f;

        dest.M21 = a.M21 * b.M11 + a.M22 * b.M21 + a.M23 * b.M31;
        dest.M22 = a.M21 * b.M12 + a.M22 * b.M22 + a.M23 * b.M32;
        dest.M23 = a.M21 * b.M13 + a.M22 * b.M23 + a.M23 * b.M33;
        dest.M24 = 0f;

        dest.M31 = a.M31 * b.M11 + a.M32 * b.M21 + a.M33 * b.M31;
        dest.M32 = a.M31 * b.M12 + a.M32 * b.M22 + a.M33 * b.M32;
        dest.M33 = a.M31 * b.M13 + a.M32 * b.M23 + a.M33 * b.M33;
        dest.M34 = 0f;

        dest.M41 = a.M41 * b.M11 + a.M42 * b.M21 + a.M43 * b.M31 + b.M41;
        dest.M42 = a.M41 * b.M12 + a.M42 * b.M22 + a.M43 * b.M32 + b.M42;
        dest.M43 = a.M41 * b.M13 + a.M42 * b.M23 + a.M43 * b.M33 + b.M43;
        dest.M44 = 1f;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void CreateModelMatrix(in Transform t, out Matrix4x4 mat)
    {
        float x = t.Rotation.X, y = t.Rotation.Y, z = t.Rotation.Z, w = t.Rotation.W;
        float xx = x + x, yy = y + y, zz = z + z;
        float xy = x * yy, xz = x * zz, yz = y * zz;
        float wx = w * xx, wy = w * yy, wz = w * zz;
        float x2 = x * xx, y2 = y * yy, z2 = z * zz;

        float r11 = 1f - (y2 + z2), r22 = 1f - (x2 + z2), r33 = 1f - (x2 + y2);
        float r12 = xy + wz, r13 = xz - wy, r21 = xy - wz;
        float r23 = yz + wx, r31 = xz + wy, r32 = yz - wx;

        // row 1 - local X Axis (Right)
        mat.M11 = r11 * t.Scale.X;
        mat.M12 = r12 * t.Scale.X;
        mat.M13 = r13 * t.Scale.X;
        mat.M14 = 0f;

        // row 2 corresponds - Local Y Axis (Up)
        mat.M21 = r21 * t.Scale.Y;
        mat.M22 = r22 * t.Scale.Y;
        mat.M23 = r23 * t.Scale.Y;
        mat.M24 = 0f;

        // row 3 corresponds - Local Z Axis (Forward)
        mat.M31 = r31 * t.Scale.Z;
        mat.M32 = r32 * t.Scale.Z;
        mat.M33 = r33 * t.Scale.Z;
        mat.M34 = 0f;

        mat.M41 = t.Translation.X;
        mat.M42 = t.Translation.Y;
        mat.M43 = t.Translation.Z;
        mat.M44 = 1f;
    }


    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void CreateModelMatrix(in Vector3 t, in Vector3 s, in Quaternion r, out Matrix4x4 mat)
    {
        float x = r.X, y = r.Y, z = r.Z, w = r.W;
        float xx = x + x, yy = y + y, zz = z + z;
        float xy = x * yy, xz = x * zz, yz = y * zz;
        float wx = w * xx, wy = w * yy, wz = w * zz;
        float x2 = x * xx, y2 = y * yy, z2 = z * zz;

        float r11 = 1f - (y2 + z2), r22 = 1f - (x2 + z2), r33 = 1f - (x2 + y2);
        float r12 = xy + wz, r13 = xz - wy, r21 = xy - wz;
        float r23 = yz + wx, r31 = xz + wy, r32 = yz - wx;

        // row 1 - local X Axis (Right)
        mat.M11 = r11 * s.X;
        mat.M12 = r12 * s.X;
        mat.M13 = r13 * s.X;
        mat.M14 = 0f;

        // row 2 corresponds - Local Y Axis (Up)
        mat.M21 = r21 * s.Y;
        mat.M22 = r22 * s.Y;
        mat.M23 = r23 * s.Y;
        mat.M24 = 0f;

        // row 3 corresponds - Local Z Axis (Forward)
        mat.M31 = r31 * s.Z;
        mat.M32 = r32 * s.Z;
        mat.M33 = r33 * s.Z;
        mat.M34 = 0f;

        mat.M41 = t.X;
        mat.M42 = t.Y;
        mat.M43 = t.Z;
        mat.M44 = 1f;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void CreateFixedSizeModelMatrix(in Vector3 t, in Quaternion r, out Matrix4x4 mat)
    {
        float x = r.X, y = r.Y, z = r.Z, w = r.W;
        float xx = x + x, yy = y + y, zz = z + z;
        float xy = x * yy, xz = x * zz, yz = y * zz;
        float wx = w * xx, wy = w * yy, wz = w * zz;
        float x2 = x * xx, y2 = y * yy, z2 = z * zz;

        float r11 = 1f - (y2 + z2), r22 = 1f - (x2 + z2), r33 = 1f - (x2 + y2);
        float r12 = xy + wz, r13 = xz - wy, r21 = xy - wz;
        float r23 = yz + wx, r31 = xz + wy, r32 = yz - wx;

        // row 1 - local X Axis (Right)
        mat.M11 = r11;
        mat.M12 = r12;
        mat.M13 = r13;
        mat.M14 = 0f;

        // row 2 corresponds - Local Y Axis (Up)
        mat.M21 = r21;
        mat.M22 = r22;
        mat.M23 = r23;
        mat.M24 = 0f;

        // row 3 corresponds - Local Z Axis (Forward)
        mat.M31 = r31;
        mat.M32 = r32;
        mat.M33 = r33;
        mat.M34 = 0f;

        mat.M41 = t.X;
        mat.M42 = t.Y;
        mat.M43 = t.Z;
        mat.M44 = 1f;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void CreateNormalMatrix(in Matrix4x4 m, out Matrix3X4 n)
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
            n.V0 = new Vector4(1, 0, 0, 0);
            n.V1 = new Vector4(0, 1, 0, 0);
            n.V2 = new Vector4(0, 0, 1, 0);
            return;
        }

        float s = 1f / det;
        n.V0 = new Vector4(C11 * s, C21 * s, C31 * s, 0f);
        n.V1 = new Vector4(C12 * s, C22 * s, C32 * s, 0f);
        n.V2 = new Vector4(C13 * s, C23 * s, C33 * s, 0f);
    }
/*

    [MethodImpl(MethodImplOptions.AggressiveInlining )]
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
    */
}