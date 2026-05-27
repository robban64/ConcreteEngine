using System.Numerics;
using System.Runtime.CompilerServices;
using System.Runtime.Intrinsics;

// ReSharper disable InconsistentNaming

namespace ConcreteEngine.Core.Common.Numerics.Maths;

public static class MatrixMath
{
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void MultiplyAffine(ref Matrix4x4 dst, in Matrix4x4 src1, in Matrix4x4 src2)
    {
        ref var b = ref Unsafe.AsRef(in src2);
        var bRow1 = Unsafe.As<float, Vector128<float>>(ref b.M11);
        var bRow2 = Unsafe.As<float, Vector128<float>>(ref b.M21);
        var bRow3 = Unsafe.As<float, Vector128<float>>(ref b.M31);
        var bRow4 = Unsafe.As<float, Vector128<float>>(ref b.M41);

        var row = Vector128.Create(src1.M11) * bRow1;
        row = Vector128.FusedMultiplyAdd(Vector128.Create(src1.M12), bRow2, row);
        row = Vector128.FusedMultiplyAdd(Vector128.Create(src1.M13), bRow3, row);
        row.StoreUnsafe(ref dst.M11);

        row = Vector128.Create(src1.M21) * bRow1;
        row = Vector128.FusedMultiplyAdd(Vector128.Create(src1.M22), bRow2, row);
        row = Vector128.FusedMultiplyAdd(Vector128.Create(src1.M23), bRow3, row);
        row.StoreUnsafe(ref dst.M21);

        row = Vector128.Create(src1.M31) * bRow1;
        row = Vector128.FusedMultiplyAdd(Vector128.Create(src1.M32), bRow2, row);
        row = Vector128.FusedMultiplyAdd(Vector128.Create(src1.M33), bRow3, row);
        row.StoreUnsafe(ref dst.M31);

        row = Vector128.FusedMultiplyAdd(Vector128.Create(src1.M41), bRow1, bRow4);
        row = Vector128.FusedMultiplyAdd(Vector128.Create(src1.M42), bRow2, row);
        row = Vector128.FusedMultiplyAdd(Vector128.Create(src1.M43), bRow3, row);
        row.StoreUnsafe(ref dst.M41);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void MultiplyAffine(ref Matrix4x4 dst, in Matrix4x4 src)
    {
        ref var b = ref Unsafe.AsRef(in src);
        var bRow1 = Unsafe.As<float, Vector128<float>>(ref b.M11);
        var bRow2 = Unsafe.As<float, Vector128<float>>(ref b.M21);
        var bRow3 = Unsafe.As<float, Vector128<float>>(ref b.M31);
        var bRow4 = Unsafe.As<float, Vector128<float>>(ref b.M41);

        var row = Vector128.Create(dst.M11) * bRow1;
        row = Vector128.FusedMultiplyAdd(Vector128.Create(dst.M12), bRow2, row);
        row = Vector128.FusedMultiplyAdd(Vector128.Create(dst.M13), bRow3, row);
        row.StoreUnsafe(ref dst.M11);

        row = Vector128.Create(dst.M21) * bRow1;
        row = Vector128.FusedMultiplyAdd(Vector128.Create(dst.M22), bRow2, row);
        row = Vector128.FusedMultiplyAdd(Vector128.Create(dst.M23), bRow3, row);
        row.StoreUnsafe(ref dst.M21);

        row = Vector128.Create(dst.M31) * bRow1;
        row = Vector128.FusedMultiplyAdd(Vector128.Create(dst.M32), bRow2, row);
        row = Vector128.FusedMultiplyAdd(Vector128.Create(dst.M33), bRow3, row);
        row.StoreUnsafe(ref dst.M31);

        row = Vector128.FusedMultiplyAdd(Vector128.Create(dst.M41), bRow1, bRow4);
        row = Vector128.FusedMultiplyAdd(Vector128.Create(dst.M42), bRow2, row);
        row = Vector128.FusedMultiplyAdd(Vector128.Create(dst.M43), bRow3, row);
        row.StoreUnsafe(ref dst.M41);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void CreateModelMatrix(in Vector3 t, in Vector3 s, in Quaternion r, out Matrix4x4 dst)
    {
        CreateModelMatrix(new Transform(in t, in s, in r), out dst);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void CreateModelMatrix(in Transform t, out Matrix4x4 dst)
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
        dst.M11 = r11 * t.Scale.X;
        dst.M12 = r12 * t.Scale.X;
        dst.M13 = r13 * t.Scale.X;
        dst.M14 = 0f;

        // row 2 corresponds - Local Y Axis (Up)
        dst.M21 = r21 * t.Scale.Y;
        dst.M22 = r22 * t.Scale.Y;
        dst.M23 = r23 * t.Scale.Y;
        dst.M24 = 0f;

        // row 3 corresponds - Local Z Axis (Forward)
        dst.M31 = r31 * t.Scale.Z;
        dst.M32 = r32 * t.Scale.Z;
        dst.M33 = r33 * t.Scale.Z;
        dst.M34 = 0f;

        dst.M41 = t.Translation.X;
        dst.M42 = t.Translation.Y;
        dst.M43 = t.Translation.Z;
        dst.M44 = 1f;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void CreateFixedSizeModelMatrix(in Vector3 t, in Quaternion r, out Matrix4x4 dst)
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
        dst.M11 = r11;
        dst.M12 = r12;
        dst.M13 = r13;
        dst.M14 = 0f;

        // row 2 corresponds - Local Y Axis (Up)
        dst.M21 = r21;
        dst.M22 = r22;
        dst.M23 = r23;
        dst.M24 = 0f;

        // row 3 corresponds - Local Z Axis (Forward)
        dst.M31 = r31;
        dst.M32 = r32;
        dst.M33 = r33;
        dst.M34 = 0f;

        dst.M41 = t.X;
        dst.M42 = t.Y;
        dst.M43 = t.Z;
        dst.M44 = 1f;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void CreateNormalMatrix(ref Matrix3X4 dst, in Matrix4x4 src)
    {
        float a = src.M11, b = src.M12, c = src.M13;
        float d = src.M21, e = src.M22, f = src.M23;
        float g = src.M31, h = src.M32, k = src.M33;

        float C11 = e * k - f * h;
        float C12 = f * g - d * k;
        float C13 = d * h - e * g;

        float C21 = c * h - b * k;
        float C22 = a * k - c * g;
        float C23 = b * g - a * h;

        float C31 = b * f - c * e;
        float C32 = c * d - a * f;
        float C33 = a * e - b * d;

        float det = MathF.FusedMultiplyAdd(a, C11, MathF.FusedMultiplyAdd(b, C12, c * C13));

        if (MathF.Abs(det) < 1e-8f)
        {
            dst = Matrix3X4.Identity;
            return;
        }

        float s = 1f / det;

        dst.M11 = C11 * s;
        dst.M12 = C21 * s;
        dst.M13 = C31 * s;

        dst.M21 = C12 * s;
        dst.M22 = C22 * s;
        dst.M23 = C32 * s;

        dst.M31 = C13 * s;
        dst.M32 = C23 * s;
        dst.M33 = C33 * s;
    }
}