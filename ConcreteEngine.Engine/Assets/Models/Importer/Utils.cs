#region

using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using ConcreteEngine.Common.Numerics;
using ConcreteEngine.Engine.Assets.Models.Loader;
using AssimpMaterial = Silk.NET.Assimp.Material;

#endregion

namespace ConcreteEngine.Engine.Assets.Models.Importer;

internal static class ModelImportUtils
{
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