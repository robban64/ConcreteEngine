using System.Numerics;
using ConcreteEngine.Common.Numerics;
using ConcreteEngine.Engine.Assets.Models.Loader;
using ConcreteEngine.Graphics.Primitives;

namespace ConcreteEngine.Engine.Assets.Models.Importer;

internal ref struct VertexWriterImporter(Span<Vertex3D> vertices, Span<uint> indices)
{
    public Span<Vertex3D> Vertices = vertices;
    public Span<uint> Indices = indices;
}

internal ref struct VertexSkinnedWriterImporter(
    Span<Vertex3DSkinned> vertices,
    Span<SkinningData> skinned,
    Span<uint> indices)
{
    public Span<Vertex3DSkinned> Vertices = vertices;
    public Span<SkinningData> Skinned = skinned;
    public Span<uint> Indices = indices;
}

internal ref struct MeshPartWriter(Span<MeshPartImportResult> parts, Span<Matrix4x4> partTransforms)
{
    public Span<MeshPartImportResult> Parts = parts;
    public Span<Matrix4x4> PartTransforms = partTransforms;

    public void Fill(int index, int materialSlot, MeshCreationInfo creationInfo, in BoundingBox bounds, ref Matrix4x4 partTransform)
    {
        ref var it = ref Parts[index];
        it.MaterialSlot = materialSlot;
        it.CreationInfo = creationInfo;
        it.Bounds = bounds;
        PartTransforms[index] = partTransform;
    }
}

internal ref struct BoneWriterImporter(Span<SkinningData> skinningData, Span<Matrix4x4> boneTransforms)
{
    public Span<SkinningData> SkinningData = skinningData;
    public Span<Matrix4x4> BoneTransforms = boneTransforms;

    public int MaxBones => BoneTransforms.Length;

    public void FillDefaultSkinningData(int? vertexCount = null)
    {
        var count = vertexCount ?? SkinningData.Length;
        var skinData = new SkinningData { BoneWeights = default, BoneIndices = new Int4(-1, -1, -1, -1) };
        SkinningData.Slice(0, count).Fill(skinData);
    }
}