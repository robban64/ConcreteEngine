using Silk.NET.Assimp;

namespace ConcreteEngine.Engine.Assets.Models.Importer;

internal static class Constants
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
    internal const int DefaultBoneTransformsCapacity = 64;
    internal const int MaxBoneTransformCapacity = 128;
    internal const int MaxParts = 8;
}