using System.Numerics;
using ConcreteEngine.Core.Common;
using ConcreteEngine.Core.Common.Collections;
using ConcreteEngine.Core.Common.Numerics;
using ConcreteEngine.Core.Common.Numerics.Primitives;
using ConcreteEngine.Engine.Assets.Loader.Data;
using ConcreteEngine.Graphics.Primitives;
using static ConcreteEngine.Engine.Assets.Loader.AssimpImporter.AssimpUtils;

namespace ConcreteEngine.Engine.Assets.Loader.State;

internal ref struct MeshVertexWriter(Span<Vertex3D> vertices, Span<uint> indices)
{
    public Span<Vertex3D> Vertices = vertices;
    public Span<uint> Indices = indices;
}

internal ref struct MeshSkinnedVertexWriter(
    Span<VertexSkinned> vertices,
    Span<SkinningData> skinned,
    Span<uint> indices)
{
    public Span<VertexSkinned> Vertices = vertices;
    public Span<SkinningData> Skinned = skinned;
    public Span<uint> Indices = indices;
}

internal ref struct MeshPartWriter(Span<MeshPartImportResult> parts, Span<Matrix4x4> partTransforms)
{
    private Span<MeshPartImportResult> _parts = parts;
    private Span<Matrix4x4> _partTransforms = partTransforms;

    public void Fill(int index, int materialSlot, MeshCreationInfo creationInfo, in BoundingBox bounds,
        in Matrix4x4 partTransform)
    {
        ref var it = ref _parts[index];
        it.MaterialSlot = materialSlot;
        it.CreationInfo = creationInfo;
        it.Bounds = bounds;
        _partTransforms[index] = partTransform;
    }
}

internal sealed class ModelLoaderDataTable
{
    private uint[] _indices = new uint[IndicesCapacity];
    private Vertex3D[] _vertices = new Vertex3D[VertexCapacity];
    private VertexSkinned[] _verticesSkinned = new VertexSkinned[VertexCapacity];
    private SkinningData[] _skinningData = new SkinningData[VertexCapacity];

    private Matrix4x4[] _boneOffsetMatrix = new Matrix4x4[BoneLimit];
    private Matrix4x4[] _nodeTransform = new Matrix4x4[BoneLimit];

    private MeshPartImportResult[] _parts = new MeshPartImportResult[MaxParts];
    private Matrix4x4[] _partTransforms = new Matrix4x4[MaxParts];


    public BoundingBox ModelBounds;
    public Matrix4x4 InvRootTransform;
    public Matrix4x4 SkeletonRootOffset;

    public Span<Matrix4x4> NodeTransforms => _nodeTransform;
    public Span<Matrix4x4> BoneOffsetMatrix => _boneOffsetMatrix;

    public ModelImportResult GetMeshDataResult(int meshCount)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(meshCount);
        return new ModelImportResult(_parts.AsSpan(0, meshCount), _partTransforms.AsSpan(0, meshCount), in ModelBounds);
    }

    public void CalculateBoundingBox(int meshCount)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(meshCount);
        AssimpImporter.AssimpUtils.CalculateBoundingBox(meshCount, _parts.AsSpan(0, meshCount), out ModelBounds);
    }

    public MeshVertexWriter WriteVertex(int vertexCount, int indexCount)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(vertexCount);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(indexCount);

        EnsureCapacity(indexCount, vertexCount: vertexCount);
        return new MeshVertexWriter(_vertices.AsSpan(0, vertexCount), _indices.AsSpan(0, indexCount));
    }

    public MeshSkinnedVertexWriter WriteVertexSkinned(int vertexCount, int indexCount)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(vertexCount);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(indexCount);

        EnsureCapacity(indexCount, skinnedCount: vertexCount);
        return new MeshSkinnedVertexWriter(
            _verticesSkinned.AsSpan(0, vertexCount),
            _skinningData.AsSpan(0, vertexCount),
            _indices.AsSpan(0, indexCount));
    }

    public MeshPartWriter WriteMeshParts() => new(_parts, _partTransforms);


    public void WriteSkinningData(int vertexCount, out Span<SkinningData> skinningData,
        out Span<Matrix4x4> boneTransforms)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(vertexCount);

        EnsureCapacity(skinnedCount: vertexCount);
        ArgumentOutOfRangeException.ThrowIfGreaterThan(vertexCount, _skinningData.Length);

        skinningData = _skinningData.AsSpan();
        boneTransforms = _boneOffsetMatrix.AsSpan();
    }


    public MeshUploadData<Vertex3D> GetUploadData(int vertexCount, int indexCount, ref MeshCreationInfo info)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(vertexCount);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(indexCount);
        ArgumentOutOfRangeException.ThrowIfGreaterThan(vertexCount, _vertices.Length);
        ArgumentOutOfRangeException.ThrowIfGreaterThan(indexCount, _indices.Length);

        return new MeshUploadData<Vertex3D>(
            _vertices.AsSpan(0, vertexCount),
            _indices.AsSpan(0, indexCount),
            ref info
        );
    }

    public MeshUploadData<VertexSkinned> GetSkinnedUploadData(int vertexCount, int indexCount,
        ref MeshCreationInfo info)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(vertexCount);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(indexCount);
        ArgumentOutOfRangeException.ThrowIfGreaterThan(vertexCount, _verticesSkinned.Length);
        ArgumentOutOfRangeException.ThrowIfGreaterThan(indexCount, _indices.Length);

        return new MeshUploadData<VertexSkinned>(
            _verticesSkinned.AsSpan(0, vertexCount),
            _indices.AsSpan(0, indexCount),
            ref info
        );
    }

    public void EnsureCapacity(int? indexCount = null, int? vertexCount = null, int? skinnedCount = null)
    {
        if (indexCount is { } iCount && (uint)indexCount > _indices.Length)
        {
            var cap = Arrays.CapacityGrowthAlign(int.Max(iCount, 64));
            Array.Resize(ref _indices, cap);
            Console.WriteLine("triggered indicies " + cap);
        }

        if (vertexCount is { } vCount && (uint)vCount > _vertices.Length)
        {
            var cap = Arrays.CapacityGrowthPow2(int.Max(vCount, 64));
            Array.Resize(ref _vertices, cap);
            Console.WriteLine("triggered verts " + cap);
        }

        if (skinnedCount is { } skinCount && (uint)skinCount > _verticesSkinned.Length)
        {
            InvalidOpThrower.ThrowIf(_skinningData.Length != _verticesSkinned.Length);
            var cap = Arrays.CapacityGrowthPow2(int.Max(skinCount, 64));
            Array.Resize(ref _verticesSkinned, cap);
            Array.Resize(ref _skinningData, cap);
            Console.WriteLine("triggered skinn " + cap);
        }
    }

    public void FillDefaultSkinningData(int vertexCount)
    {
        var skinData = new SkinningData { BoneWeights = default, BoneIndices = new Int4(-1, -1, -1, -1) };
        _skinningData.AsSpan(0, int.Max(vertexCount, _skinningData.Length)).Fill(skinData);
    }


    public void Clear()
    {
        Array.Clear(_parts);
        Array.Clear(_partTransforms);
        Array.Fill(_nodeTransform, Matrix4x4.Identity);
        Array.Fill(_nodeTransform, Matrix4x4.Identity);

        ModelBounds = default;
        InvRootTransform = Matrix4x4.Identity;
        SkeletonRootOffset = Matrix4x4.Identity;
    }

    public void Teardown()
    {
        _indices = null!;
        _vertices = null!;
        _verticesSkinned = null!;
        _nodeTransform = null!;
        _boneOffsetMatrix = null!;
        _skinningData = null!;
        _parts = null!;
        _partTransforms = null!;
    }
}