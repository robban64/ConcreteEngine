using System.Numerics;
using ConcreteEngine.Common;
using ConcreteEngine.Common.Collections;
using ConcreteEngine.Common.Numerics;
using ConcreteEngine.Engine.Assets.Models.Loader;
using ConcreteEngine.Graphics.Primitives;
using static ConcreteEngine.Engine.Assets.Models.Importer.Constants;

namespace ConcreteEngine.Engine.Assets.Models.Importer;

internal sealed class ModelImportDataStore
{
    private uint[] _indices = new uint[DefaultCapacity];
    private Vertex3D[] _vertices = new Vertex3D[DefaultCapacity];
    private Vertex3DSkinned[] _verticesSkinned = new Vertex3DSkinned[DefaultCapacity];

    private Matrix4x4[] _boneTransforms = new Matrix4x4[DefaultBoneTransformsCapacity];
    private SkinningData[] _skinningData = new SkinningData[DefaultCapacity];

    private MeshPartImportResult[] _parts = new MeshPartImportResult[MaxParts];
    private Matrix4x4[] _partTransforms = new Matrix4x4[MaxParts];
    
    private BoundingBox _modelBounds;
    private Matrix4x4 _invRootTransform;

    public ref BoundingBox ModelBounds => ref _modelBounds;
    public ref Matrix4x4 InvRootTransform => ref _invRootTransform;
    public ReadOnlySpan<MeshPartImportResult> GetParts(int length) => _parts.AsSpan(0,length);

    /*
    public ModelImportResult GetModelImportResult(int meshCount)
    {
        var parts = _parts.AsSpan(0, meshCount);
        var partTransforms = _partTransforms.AsSpan(0, meshCount);
        return new ModelImportResult(_parts.AsSpan(0,meshCount), _partTransforms, ref _modelBounds);
    }
    
    public AnimationImportResult GetAnimationImportData(int meshCount)
    {
        var parts = _parts.AsSpan(0, meshCount);
        var partTransforms = _partTransforms.AsSpan(0, meshCount);
        return new AnimationImportResult(_parts.AsSpan(0,meshCount), _partTransforms, ref _modelBounds);
    }
*/

    public VertexWriterImporter WriteVertex(int vertexCount, int indexCount)
    {
        EnsureCapacity(indexCount, vertexCount: vertexCount);
        return new VertexWriterImporter(_vertices.AsSpan(0, vertexCount), _indices.AsSpan(0, indexCount));
    }

    public VertexSkinnedWriterImporter WriteVertexSkinned(int vertexCount, int indexCount)
    {
        EnsureCapacity(indexCount, skinnedCount: vertexCount);
        return new VertexSkinnedWriterImporter(
            _verticesSkinned.AsSpan(0, vertexCount),
            _skinningData.AsSpan(0, vertexCount),
            _indices.AsSpan(0, indexCount));
    }
    
    public MeshPartWriter WriteMeshParts() => new(_parts, _partTransforms);


    public BoneWriterImporter WriteBones(int? vertexCount = null)
    {
        int vCount  = vertexCount ?? _skinningData.Length;
        ArgumentOutOfRangeException.ThrowIfGreaterThan(vCount, _skinningData.Length);

        return new BoneWriterImporter(
            _skinningData.AsSpan(0, vCount),
            _boneTransforms.AsSpan());
    }


    public MeshUploadData<Vertex3D> GetUploadData(int vertexCount, int indexCount, ref MeshCreationInfo info)
    {
        ArgumentOutOfRangeException.ThrowIfGreaterThan(vertexCount, _vertices.Length);
        ArgumentOutOfRangeException.ThrowIfGreaterThan(indexCount, _indices.Length);

        return new MeshUploadData<Vertex3D>(
            _vertices.AsSpan(0, vertexCount),
            _indices.AsSpan(0, indexCount),
            ref info
        );
    }

    public MeshUploadData<Vertex3DSkinned> GetSkinnedUploadData(int vertexCount, int indexCount,
        ref MeshCreationInfo info)
    {
        ArgumentOutOfRangeException.ThrowIfGreaterThan(vertexCount, _verticesSkinned.Length);
        ArgumentOutOfRangeException.ThrowIfGreaterThan(indexCount, _indices.Length);

        return new MeshUploadData<Vertex3DSkinned>(
            _verticesSkinned.AsSpan(0, vertexCount),
            _indices.AsSpan(0, indexCount),
            ref info
        );
    }

    public void EnsureCapacity(int indexCount, int? vertexCount = null, int? skinnedCount = null)
    {
        if ((uint)indexCount > _indices.Length)
        {
            var cap = ArrayUtility.CapacityGrowthPow2(int.Max(indexCount, 64));
            Array.Resize(ref _indices, cap);
        }

        if (vertexCount is { } vCount && (uint)vCount > _vertices.Length)
        {
            var cap = ArrayUtility.CapacityGrowthPow2(int.Max(vCount, 64));
            Array.Resize(ref _vertices, cap);
        }

        if (vertexCount is { } skinCount && (uint)skinCount > _verticesSkinned.Length)
        {
            InvalidOpThrower.ThrowIf(_skinningData.Length != _verticesSkinned.Length);
            
            var cap = ArrayUtility.CapacityGrowthPow2(int.Max(skinCount, 64));
            Array.Resize(ref _verticesSkinned, cap);
            Array.Resize(ref _skinningData, cap);
        }
    }
    


    public void Clear()
    {
        _boneTransforms.AsSpan().Clear();
        _skinningData.AsSpan().Clear();
        _modelBounds = default;
        _invRootTransform = Matrix4x4.Identity;
    }

    public void Unload()
    {
        _indices = null!;
        _vertices = null!;
        _verticesSkinned = null!;
        _boneTransforms = null!;
        _skinningData = null!;
        _parts = null!;
        _partTransforms = null!;
    }
}