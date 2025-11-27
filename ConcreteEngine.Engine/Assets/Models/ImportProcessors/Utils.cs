#region

using System.Numerics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using ConcreteEngine.Common.Numerics;
using ConcreteEngine.Common.Numerics.Maths;
using ConcreteEngine.Engine.Assets.Models.Loader;
using AssimpMaterial = Silk.NET.Assimp.Material;

#endregion

namespace ConcreteEngine.Engine.Assets.Models.ImportProcessors;

internal static class ImportUtils
{
    public static float GetMaxElementAbs(this Matrix4x4 m)
    {
        float max = float.MinValue;
        for (int i = 0; i < 4; i++)
        {
            for (int j = 0; j < 4; j++)
            {
                var v = m.GetElement(i,j);
                if (v > max) max = v;
            }
        }
        return max;

    }
  

    public static Matrix4x4 SanitizeAssimpMatrix(Matrix4x4 mat)
    {
        /*
        var fixedMat = new Matrix4x4(
            mat.M11, mat.M12, mat.M13, 0f, // Row 1: Rotation X
            mat.M21, mat.M22, mat.M23, 0f, // Row 2: Rotation Y
            mat.M31, mat.M32, mat.M33, 0f, // Row 3: Rotation Z
            mat.M14, mat.M24, mat.M34, 1f // Row 4: Translation 
        );

        if (Matrix4x4.Decompose(fixedMat, out var scale, out var rot, out var trans))
        {
            return Matrix4x4.CreateScale(scale) * Matrix4x4.CreateFromQuaternion(rot) *
                   Matrix4x4.CreateTranslation(trans);
        }

        return fixedMat;
        */
        return mat;
    }

    public static Vector3 TransformZupToYup(in Vector3 v) => new(v.X, v.Z, -v.Y);

    // Vector3 from Y-up to Z-up
    public static Vector3 TransformYupToZup(in Vector3 v) => new(v.X, -v.Z, v.Y);

    // 90 degrees around X-axis (Z-up to Y-up)
    public static readonly Matrix4x4 ZupToYup = new(
        1, 0, 0, 0,
        0, 0, 1, 0,
        0, -1, 0, 0,
        0, 0, 0, 1
    );

    // -90 degrees around X-axis (Y-up to Z-up)
    public static readonly Matrix4x4 YupToZup = new(
        1, 0, 0, 0,
        0, 0, -1, 0,
        0, 1, 0, 0,
        0, 0, 0, 1
    );

    //  90-degree rotation around X
    public static readonly Quaternion ZupToYupRotation =
        Quaternion.CreateFromAxisAngle(Vector3.UnitX, FloatMath.ToRadians(90));

    public static void CalculateBoundingBox(int count, ReadOnlySpan<MeshPartImportResult> parts, out BoundingBox bounds)
    {
        bounds = parts[0].Bounds;
        for (var i = 1; i < count; i++)
            BoundingBox.Merge(in bounds, in parts[i].Bounds, out bounds);
    }

    // cm->0.01f, mm->0.001f, m->1f
    public static float DecideScale(in BoundingBox bounds, float unitScale)
    {
        var size = bounds.Max - bounds.Min;
        var maxDim = MathF.Max(size.X, MathF.Max(size.Y, size.Z));
        return unitScale * (maxDim > 100f ? 0.01f : maxDim < 0.01f ? 0.001f : 1f);
    }

    public static unsafe void DumpMaterialProperties(AssimpMaterial* material)
    {
        if (material == null) return;
        for (var i = 0; i < material->MNumProperties; i++)
        {
            var p = material->MProperties[i];
            if (p == null) continue;
            int len = (int)p->MDataLength;
            string key = p->MKey.AsString;
            Console.WriteLine(
                $"Prop[{i}] Key={key}, Semantic={p->MSemantic}, Index={p->MIndex}, Type={p->MType}, Length={len}");
            if (len > 0 && p->MData != null)
            {
                ref byte b0 = ref Unsafe.AsRef<byte>(p->MData);
                var slice = MemoryMarshal.CreateReadOnlySpan(ref b0, Math.Min(len, 64));
                Console.WriteLine($"  FirstBytes(hex) = {BitConverter.ToString(slice.ToArray())}");
            }
        }
    }
}