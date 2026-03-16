using ConcreteEngine.Core.Common.Numerics;
using Silk.NET.Assimp;
using AssimpScene = Silk.NET.Assimp.Scene;

namespace ConcreteEngine.Engine.Assets.Loader.ImporterAssimp;

internal struct AssimpSceneMeta
{
    public ushort MeshCount;
    public ushort BoneCount;
    public ushort AnimationCount;
    public ushort MaterialCount;
    public ushort TextureCount;

    public unsafe void FromScene(AssimpScene* scene, int boneCount)
    {
        BoneCount = (ushort)boneCount;
        MeshCount = (ushort)scene->MNumMeshes;
        AnimationCount = (ushort)scene->MNumAnimations;
        MaterialCount = (ushort)scene->MNumMaterials;
        TextureCount = (ushort)scene->MNumTextures;
    }
}

internal static class AssimpUtils
{
    public const PostProcessSteps AssimpFlags =
        PostProcessSteps.Triangulate |
        PostProcessSteps.SortByPrimitiveType |
        PostProcessSteps.JoinIdenticalVertices |
        PostProcessSteps.FixInFacingNormals |
        PostProcessSteps.ImproveCacheLocality |
        PostProcessSteps.CalculateTangentSpace |
        PostProcessSteps.OptimizeMeshes |
        PostProcessSteps.FlipUVs |
        PostProcessSteps.LimitBoneWeights |
        PostProcessSteps.ValidateDataStructure;
        //PostProcessSteps.GenerateBoundingBoxes;

    public const int BoneLimit = 64;

    public static unsafe uint GetNameHash(AssimpString str) => GetNameHash(str.Data, str.Length);

    public static unsafe uint GetNameHash(byte* data, uint length)
    {
        uint hash = 2166136261;
        for (int i = 0; i < length; i++)
            hash = (hash ^ data[i]) * 16777619;
        return hash;
    }

    public static float DecideScale(in BoundingBox bounds, float unitScale)
    {
        var size = bounds.Max - bounds.Min;
        var maxDim = MathF.Max(size.X, MathF.Max(size.Y, size.Z));
        return unitScale * (maxDim > 100f ? 0.01f : maxDim < 0.01f ? 0.001f : 1f);
    }
}