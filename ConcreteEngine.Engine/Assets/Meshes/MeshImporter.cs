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

    private ModelImportResult _result = new();

    private readonly Func<MeshImportData, MeshCreationInfo> _onProcess;

    internal MeshImporter(Func<MeshImportData, MeshCreationInfo> onProcess)
    {
        _onProcess = onProcess;
    }


    public void ClearCache()
    {
        _result = null!;

        Array.Resize(ref _verts, 0);
        Array.Resize(ref _indices, 0);

        _assimp?.Dispose();
        _assimp = null;
    }

    public unsafe ModelImportResult ImportMesh(string path, out Span<MeshPartImportResult> parts,
        out Span<Matrix4x4> partTransforms)
    {
        if (_assimp == null)
            _assimp = Assimp.GetApi();

        var scene = _assimp.ImportFile(path, (uint)Steps);

        if (scene == null || scene->MFlags == Assimp.SceneFlagsIncomplete || scene->MRootNode == null)
        {
            var error = _assimp.GetErrorStringS();
            throw new InvalidOperationException(error);
        }

        _result.PartNames.Clear();
        TraverseNode(scene->MRootNode, scene, 0, Matrix4x4.Identity, _parts, _partTransforms, _result.PartNames);
        _result.Parts = _result.PartNames.Count;

        InvalidOpThrower.ThrowIf(_result.Parts > _parts.Length, nameof(_result.Parts));
        InvalidOpThrower.ThrowIf(_result.Parts != _result.PartNames.Count, nameof(_result.Parts));

        parts = _parts.AsSpan(0, _result.Parts);
        partTransforms = _partTransforms.AsSpan(0, _result.Parts);

        BoundingBox bounds = default;
        for (var i = 0; i < _result.Parts; i++)
        {
            if (i == 0)
            {
                bounds = parts[i].Bounds;
                continue;
            }

            BoundingBox.Merge(in bounds, in parts[i].Bounds, out bounds);
        }

        _result.Bounds = bounds;
        return _result;
    }

    private unsafe void TraverseNode(AssimpNode* node, AssimpScene* scene, int index, in Matrix4x4 parent,
        Span<MeshPartImportResult> parts, Span<Matrix4x4> partTransforms, List<string> names)
    {
        var current = node->MTransformation * parent;
        for (var i = 0; i < node->MNumMeshes; i++)
        {
            var meshIndex = node->MMeshes[i];
            var mesh = scene->MMeshes[meshIndex];
            var meshData = LoadMeshData(mesh);

            BoundingBox.FromPoints(new Span<Vector3>(mesh->MVertices, (int)mesh->MNumVertices), out var box);

            ref var it = ref parts[index + i];
            it.MaterialSlot = (int)scene->MMeshes[i]->MMaterialIndex;
            it.CreationInfo = meshData;
            it.Bounds = box;
            partTransforms[i] = current;
            names.Add(mesh->MName.AsString);
        }

        var idx = index + (int)node->MNumMeshes;

        // Process children
        for (uint i = 0; i < node->MNumChildren; i++)
        {
            TraverseNode(node->MChildren[i], scene, idx, in current, parts, partTransforms, names);
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
            v.Normal = mesh->MNormals != null ? mesh->MNormals[i] : default;
            v.Tangent = mesh->MTangents != null ? mesh->MTangents[i] : default;
            v.TexCoords = mesh->MTextureCoords[0] != null ? v.TexCoords = mesh->MTextureCoords[0][i].ToVec2() : default;
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