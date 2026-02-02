using ConcreteEngine.Core.Common.Numerics;
using Silk.NET.Assimp;
using AssimpScene = Silk.NET.Assimp.Scene;
using AssimpNode = Silk.NET.Assimp.Node;
using AssimpMesh = Silk.NET.Assimp.Mesh;
using AssimpAnimation = Silk.NET.Assimp.Animation;

namespace ConcreteEngine.Engine.Assets.Loader.ImporterModel;

internal struct AssimpSceneMeta
{
    public int MeshCount;
    public int BoneCount;
    public int AnimationCount;
    public int MaterialCount;
    public int TextureCount;

    public unsafe void FromScene(AssimpScene* scene, int boneCount)
    {
        BoneCount = boneCount;
        MeshCount = (int)scene->MNumMeshes;
        AnimationCount = (int)scene->MNumAnimations;
        MaterialCount = (int)scene->MNumMaterials;
        TextureCount = (int)scene->MNumTextures;
    }
}

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