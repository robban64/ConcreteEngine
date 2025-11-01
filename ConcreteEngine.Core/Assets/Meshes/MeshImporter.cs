#region

using System.Numerics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using ConcreteEngine.Common.Collections;
using ConcreteEngine.Common.Numerics.Extensions;
using ConcreteEngine.Graphics.Primitives;
using Silk.NET.Assimp;
using AssimpMesh = Silk.NET.Assimp.Mesh;
using AssimpScene = Silk.NET.Assimp.Scene;
using AssimpNode = Silk.NET.Assimp.Node;

#endregion

namespace ConcreteEngine.Core.Assets.Meshes;

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

    private readonly List<MeshPartImportResult> _resultInfo = new(4);

    private readonly Func<MeshImportData, MeshCreationInfo> _onProcess;

    internal MeshImporter(Func<MeshImportData, MeshCreationInfo> onProcess)
    {
        _onProcess = onProcess;
    }


    public void ClearCache()
    {
        _resultInfo.Clear();
        _resultInfo.TrimExcess();

        Array.Resize(ref _verts, 0);
        Array.Resize(ref _indices, 0);

        _assimp?.Dispose();
        _assimp = null;
    }

    public unsafe MeshPartImportResult[] ImportMesh(string path)
    {
        if (_assimp == null)
            _assimp = Assimp.GetApi();

        var scene = _assimp.ImportFile(path, (uint)Steps);

        if (scene == null || scene->MFlags == Assimp.SceneFlagsIncomplete || scene->MRootNode == null)
        {
            var error = _assimp.GetErrorStringS();
            throw new InvalidOperationException(error);
        }

        _resultInfo.Clear();

        TraverseNode(scene->MRootNode, Matrix4x4.Identity, scene);

        return _resultInfo.ToArray();
    }

    private unsafe void TraverseNode(AssimpNode* node, Matrix4x4 parent, AssimpScene* scene)
    {
        var current = node->MTransformation * parent;
        for (var i = 0; i < node->MNumMeshes; i++)
        {
            var meshIndex = node->MMeshes[i];
            var mesh = scene->MMeshes[meshIndex];
            var meshData = LoadMeshData(mesh);

            _resultInfo.Add(new MeshPartImportResult(
                mesh->MName.AsString,
                (int)scene->MMeshes[i]->MMaterialIndex,
                meshData,
                current
            ));
        }

        // Process children
        for (uint i = 0; i < node->MNumChildren; i++)
        {
            TraverseNode(node->MChildren[i], current, scene);
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

    private unsafe void ComputeBBox(AssimpMesh* mesh, out Vector3 min, out Vector3 max)
    {
        min = new Vector3(float.PositiveInfinity);
        max = new Vector3(float.NegativeInfinity);
        for (uint i = 0; i < mesh->MNumVertices; i++)
        {
            var p = mesh->MVertices[i];
            min = Vector3.Min(min, p);
            max = Vector3.Max(max, p);
        }
    }

    // cm->0.01f, mm->0.001f, m->1f
    private static float DecideScale(Vector3 bboxMin, Vector3 bboxMax, float unitScale)
    {
        var size = bboxMax - bboxMin;
        var maxDim = MathF.Max(size.X, MathF.Max(size.Y, size.Z));
        return unitScale * (maxDim > 100f ? 0.01f : maxDim < 0.01f ? 0.001f : 1f);
    }
}