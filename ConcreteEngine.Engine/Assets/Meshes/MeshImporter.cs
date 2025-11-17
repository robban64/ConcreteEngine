#region

using System.Numerics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using ConcreteEngine.Common;
using ConcreteEngine.Common.Collections;
using ConcreteEngine.Common.Numerics;
using ConcreteEngine.Common.Numerics.Extensions;
using ConcreteEngine.Graphics.Primitives;
using Silk.NET.Assimp;
using AssimpMesh = Silk.NET.Assimp.Mesh;
using AssimpScene = Silk.NET.Assimp.Scene;
using AssimpNode = Silk.NET.Assimp.Node;

#endregion

namespace ConcreteEngine.Engine.Assets.Meshes;

internal sealed class MeshImporter
{
    //PostProcessSteps.PreTransformVertices | 
    private const PostProcessSteps Steps =
        PostProcessSteps.Triangulate |
        PostProcessSteps.SortByPrimitiveType |
        PostProcessSteps.JoinIdenticalVertices |
        PostProcessSteps.GenerateSmoothNormals |
        PostProcessSteps.ImproveCacheLocality |
        PostProcessSteps.CalculateTangentSpace |
        PostProcessSteps.OptimizeMeshes |
        PostProcessSteps.FlipUVs;


    private Assimp? _assimp;

    private Vertex3D[] _verts = new Vertex3D[2048];
    private uint[] _indices = new uint[2048];

    private readonly MeshPartImportResult[] _parts = new MeshPartImportResult[8];
    private readonly Matrix4x4[] _partTransforms = new Matrix4x4[8];
    private readonly List<string> _names = new(8);


    private readonly Func<MeshImportData, MeshCreationInfo> _onProcess;

    internal MeshImporter(Func<MeshImportData, MeshCreationInfo> onProcess)
    {
        _onProcess = onProcess;
    }


    public void ClearCache()
    {

        _names.Clear();
        
        Array.Resize(ref _verts, 0);
        Array.Resize(ref _indices, 0);

        _assimp?.Dispose();
        _assimp = null;
    }

    public unsafe ModelImportResult ImportMesh(string path)
    {
        if (_assimp == null)
            _assimp = Assimp.GetApi();

        var scene = _assimp.ImportFile(path, (uint)Steps);

        if (scene == null || scene->MFlags == Assimp.SceneFlagsIncomplete || scene->MRootNode == null)
        {
            var error = _assimp.GetErrorStringS();
            throw new InvalidOperationException(error);
        }
        
        //cleanup
        _names.Clear();
        
        TraverseNode(scene->MRootNode, scene, 0, Matrix4x4.Identity);

        var count = _names.Count;
        InvalidOpThrower.ThrowIf(count > _parts.Length, nameof(_parts));
        InvalidOpThrower.ThrowIf(count > _partTransforms.Length, nameof(_partTransforms));

        var parts = _parts.AsSpan(0, count);
        var partTransforms = _partTransforms.AsSpan(0, count);

        BoundingBox bounds = default;
        for (var i = 0; i < count; i++)
        {
            if (i == 0)
            {
                bounds = parts[i].Bounds;
                continue;
            }

            BoundingBox.Merge(in bounds, in parts[i].Bounds, out bounds);
        }

        return new ModelImportResult(CollectionsMarshal.AsSpan(_names), parts, partTransforms, bounds);
    }

    private unsafe void TraverseNode(AssimpNode* node, AssimpScene* scene, int index, in Matrix4x4 parent)
    {
        var current = node->MTransformation * parent;
        for (var i = 0; i < node->MNumMeshes; i++)
        {
            var meshIndex = node->MMeshes[i];
            var mesh = scene->MMeshes[meshIndex];
            var meshData = LoadMeshData(mesh);

            BoundingBox.FromPoints(new Span<Vector3>(mesh->MVertices, (int)mesh->MNumVertices), out var box);

            ref var it = ref _parts[index + i];
            it.MaterialSlot = (int)scene->MMeshes[i]->MMaterialIndex;
            it.CreationInfo = meshData;
            it.Bounds = box;
            _partTransforms[i] = current;
            _names.Add(mesh->MName.AsString);
        }

        var idx = index + (int)node->MNumMeshes;

        // Process children
        for (uint i = 0; i < node->MNumChildren; i++)
        {
            TraverseNode(node->MChildren[i], scene, idx, in current);
        }
    }


    private unsafe MeshCreationInfo LoadMeshData(AssimpMesh* mesh)
    {
        var vertexCount = (int)mesh->MNumVertices;
        var indexCount = (int)(mesh->MNumFaces * 3);
        EnsureCapacity(vertexCount, indexCount);

        var vertices = _verts.AsSpan(0, vertexCount);
        ref var v0 = ref MemoryMarshal.GetReference(vertices);

        for (uint i = 0; i < mesh->MNumVertices; i++)
        {
            ref var v = ref Unsafe.Add(ref v0, i);
            v.Position = mesh->MVertices[i];
            v.Normal = mesh->MNormals[i];
            v.Tangent = mesh->MTangents[i];
            v.TexCoords = v.TexCoords = mesh->MTextureCoords[0][i].ToVec2();
        }

        var indices = _indices.AsSpan(0, indexCount);
        var idx = 0;
        for (uint i = 0; i < mesh->MNumFaces; i++)
        {
            var face = mesh->MFaces[i];
            indices[idx++] = face.MIndices[0];
            indices[idx++] = face.MIndices[1];
            indices[idx++] = face.MIndices[2];
        }


        var vRes = _verts.AsSpan(0, vertexCount);
        var iRes = _indices.AsSpan(0, indexCount);

        BoundingBox.FromPoints(new Span<Vector3>(mesh->MVertices, (int)mesh->MNumVertices), out var box);

        return _onProcess(new MeshImportData(vRes, iRes));
    }

    private void EnsureCapacity(int vertexCount, int indexCount)
    {
        if (_verts.Length < vertexCount)
        {
            var cap = ArrayUtility.CapacityGrowthPow2(int.Max(vertexCount, 8));
            Array.Resize(ref _verts, cap);
        }

        if (_indices.Length < indexCount)
        {
            var cap = ArrayUtility.CapacityGrowthPow2(int.Max(indexCount, 8));
            Array.Resize(ref _indices, cap);
        }
    }

    private unsafe void ComputeBBox(AssimpMesh* mesh, out BoundingBox box)
    {
        BoundingBox.FromPoints(new Span<Vector3>(mesh->MVertices, (int)mesh->MNumVertices), out box);
    }

    // cm->0.01f, mm->0.001f, m->1f
    private static float DecideScale(Vector3 bboxMin, Vector3 bboxMax, float unitScale)
    {
        var size = bboxMax - bboxMin;
        var maxDim = MathF.Max(size.X, MathF.Max(size.Y, size.Z));
        return unitScale * (maxDim > 100f ? 0.01f : maxDim < 0.01f ? 0.001f : 1f);
    }
}