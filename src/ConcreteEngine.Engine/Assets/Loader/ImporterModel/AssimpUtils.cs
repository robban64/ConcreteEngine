using ConcreteEngine.Core.Common.Numerics;
using Silk.NET.Assimp;

namespace ConcreteEngine.Engine.Assets.Loader.ImporterModel;

internal static class AssimpUtils
{
    public const PostProcessSteps AssimpFlags =
        PostProcessSteps.Triangulate |
        PostProcessSteps.SortByPrimitiveType |
        PostProcessSteps.JoinIdenticalVertices |
        PostProcessSteps.GenerateSmoothNormals |
        PostProcessSteps.ImproveCacheLocality |
        PostProcessSteps.CalculateTangentSpace |
        PostProcessSteps.OptimizeMeshes |
        PostProcessSteps.FlipUVs |
        PostProcessSteps.LimitBoneWeights;
    
    public const int BoneLimit = 64;

    public static float DecideScale(in BoundingBox bounds, float unitScale)
    {
        var size = bounds.Max - bounds.Min;
        var maxDim = MathF.Max(size.X, MathF.Max(size.Y, size.Z));
        return unitScale * (maxDim > 100f ? 0.01f : maxDim < 0.01f ? 0.001f : 1f);
    }

}