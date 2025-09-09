using System.Numerics;
using System.Runtime.InteropServices;
using ConcreteEngine.Graphics.Descriptors;
using ConcreteEngine.Graphics.Primitives;
using Silk.NET.Assimp;
using AssimpMesh = Silk.NET.Assimp.Mesh;

namespace ConcreteEngine.Core.Assets;

internal sealed class MeshImporter
{
    private Assimp? _assimp;
    private readonly List<Vertex3D> _vertices = new(1024);
    private readonly List<uint> _indices = new(1024);

    //private Vertex3D[] _verticesBuffer = [];
    //private uint[] _indicesBuffer = [];


    public void ClearCache()
    {
        _vertices.Clear();
        _indices.Clear();
        _vertices.TrimExcess();
        _indices.TrimExcess();

        //Array.Resize(ref _verticesBuffer, 0);
        //Array.Resize(ref _indicesBuffer, 0);


        _assimp?.Dispose();
        _assimp = null;
    }

    public unsafe GpuMeshData<Vertex3D, uint> ImportMesh(string path)
    {
        if (_assimp == null)
            _assimp = Assimp.GetApi();

        var steps =
            PostProcessSteps.Triangulate |
            PostProcessSteps.SortByPrimitiveType |
            PostProcessSteps.GenerateSmoothNormals |
            PostProcessSteps.CalculateTangentSpace |
            PostProcessSteps.PreTransformVertices | // bake nodes
            PostProcessSteps.FlipUVs;


        var scene = _assimp.ImportFile(path, (uint)steps);

        if (scene == null || scene->MFlags == Assimp.SceneFlagsIncomplete || scene->MRootNode == null)
        {
            var error = _assimp.GetErrorStringS();
            throw new Exception(error);
        }

        if (scene->MNumMeshes > 1)
            throw new NotSupportedException($"{path} have several meshes, only one mesh is supported atm.");

        var mesh = scene->MMeshes[0];
        return LoadMeshData(mesh);
    }


    private unsafe GpuMeshData<Vertex3D, uint> LoadMeshData(AssimpMesh* mesh, float scaleFactor = 1)
    {
        int vertexCount = (int)mesh->MNumVertices;
        int indexCount = (int)(mesh->MNumFaces * 3);

        _vertices.Clear();
        _indices.Clear();
        if (_vertices.Count < vertexCount)
            _vertices.EnsureCapacity(vertexCount);
        if (_indices.Count < vertexCount)
            _indices.EnsureCapacity(indexCount);

        ComputeBBox(mesh, out var bboxMin, out var bboxMax);
        float scale = DecideScale(bboxMin, bboxMax);
        var offset = (bboxMin + bboxMax) * 0.5f;

        for (uint i = 0; i < mesh->MNumVertices; i++)
        {
            var vertex = new Vertex3D();
            var pos = mesh->MVertices[i];
            vertex.Position = (pos - offset) * scale;

            // texture coordinates
            if (mesh->MTextureCoords[0] != null)
            {
                var texcoord3 = mesh->MTextureCoords[0][i];
                vertex.TexCoords = new Vector2(texcoord3.X, texcoord3.Y);
            }

            // normals
            if (mesh->MNormals != null)
                vertex.Normal = mesh->MNormals[i];

            // tangent
            if (mesh->MTangents != null)
                vertex.Tangent = mesh->MTangents[i];

            // bitangent (not used)
            // if (mesh->MBitangents != null)
            // vertex.Bitangent = mesh->MBitangents[i];

            _vertices.Add(vertex);
        }

        for (uint i = 0; i < mesh->MNumFaces; i++)
        {
            var face = mesh->MFaces[i];
            _indices.Add(face.MIndices[0]);
            _indices.Add(face.MIndices[1]);
            _indices.Add(face.MIndices[2]);
        }

        /*
        if(_verticesBuffer.Length < _vertices.Count)
            Array.Resize(ref _verticesBuffer, Math.Max(_vertices.Count, _verticesBuffer.Length * 2));

        if(_indicesBuffer.Length < _indices.Count)
            Array.Resize(ref _indicesBuffer, Math.Max(_indices.Count, _indicesBuffer.Length * 2));
        
        
        CollectionsMarshal.AsSpan(_vertices).CopyTo(_verticesBuffer.AsSpan(0, _vertices.Count));
        CollectionsMarshal.AsSpan(_indices).CopyTo(_indicesBuffer.AsSpan(0, _indices.Count));

        var verticesRes = _verticesBuffer.AsMemory(0, _vertices.Count);
        var indicesRes = _indicesBuffer.AsMemory(0, _indices.Count);
*/
        var vert = CollectionsMarshal.AsSpan(_vertices);
        var indices = CollectionsMarshal.AsSpan(_indices);
        return new GpuMeshData<Vertex3D, uint>(vert, indices);
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

    private static float DecideScale(in Vector3 bboxMin, in Vector3 bboxMax)
    {
        var size = bboxMax - bboxMin;
        var maxDim = MathF.Max(size.X, MathF.Max(size.Y, size.Z));

        float unitScale = 1f; // cm->0.01f, mm->0.001f, m->1f

        return unitScale * ((maxDim > 100f) ? 0.01f : (maxDim < 0.01f ? 0.001f : 1f));
    }
}