#region

using ConcreteEngine.Common.Numerics;
using Silk.NET.Assimp;

#endregion

namespace ConcreteEngine.Engine.Assets.Models.Loader.AssimpImporter;

internal static class ImportModelUtils
{
    internal const PostProcessSteps AssimpFlags =
        PostProcessSteps.Triangulate |
        PostProcessSteps.SortByPrimitiveType |
        PostProcessSteps.JoinIdenticalVertices |
        PostProcessSteps.GenerateSmoothNormals |
        PostProcessSteps.ImproveCacheLocality |
        PostProcessSteps.CalculateTangentSpace |
        PostProcessSteps.OptimizeMeshes |
        PostProcessSteps.FlipUVs |
        PostProcessSteps.LimitBoneWeights;


    internal const int DefaultCapacity = 8192;
    internal const int BoneTransformsCapacity = 64;
    internal const int MaxParts = 6;

    internal static void CalculateBoundingBox(int count, ReadOnlySpan<MeshPartImportResult> parts,
        out BoundingBox bounds)
    {
        bounds = parts[0].Bounds;
        for (var i = 1; i < count; i++)
            BoundingBox.Merge(in bounds, in parts[i].Bounds, out bounds);
    }

    // cm->0.01f, mm->0.001f, m->1f
    internal static float DecideScale(in BoundingBox bounds, float unitScale)
    {
        var size = bounds.Max - bounds.Min;
        var maxDim = MathF.Max(size.X, MathF.Max(size.Y, size.Z));
        return unitScale * (maxDim > 100f ? 0.01f : maxDim < 0.01f ? 0.001f : 1f);
    }
}