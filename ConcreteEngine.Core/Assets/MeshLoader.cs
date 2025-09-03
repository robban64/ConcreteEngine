using System.Numerics;
using ConcreteEngine.Graphics.Primitives;
using Silk.NET.Assimp;
using AssimpMesh = Silk.NET.Assimp.Mesh;

namespace ConcreteEngine.Core.Assets;

internal sealed class MeshLoader
{
    private Assimp? _assimp;
    private readonly List<Vertex3D> _vertices = new(1024);
    private readonly List<uint> _indices = new(1024);
    
    private Vertex3D[] _verticesBuffer;
    private uint[] _indicesBuffer;
    
    
    public void ClearCache()
    {
        _vertices.Clear();
        _indices.Clear();
        _vertices.TrimExcess();
        _indices.TrimExcess();
        
        Array.Resize(ref _verticesBuffer, 0);
        Array.Resize(ref _indicesBuffer, 0);
        

        _assimp?.Dispose();
        _assimp = null;
    }

    public unsafe MeshData LoadModel(string path)
    {
        if(_assimp == null)
            _assimp = Assimp.GetApi();
        
        var steps = PostProcessSteps.Triangulate | PostProcessSteps.SortByPrimitiveType | PostProcessSteps.JoinIdenticalVertices | PostProcessSteps.GenerateSmoothNormals
                                 | PostProcessSteps.CalculateTangentSpace | PostProcessSteps.LimitBoneWeights | PostProcessSteps.FlipUVs;
        var scene = _assimp.ImportFile(path, (uint)steps);
        
        if (scene == null || scene->MFlags == Silk.NET.Assimp.Assimp.SceneFlagsIncomplete || scene->MRootNode == null)
        {
            var error = _assimp.GetErrorStringS();
            throw new Exception(error);
        }
        
        if (scene->MNumMeshes > 1)
            throw new NotSupportedException($"{path} have several meshes, only one mesh is supported atm.");
        
        var mesh = scene->MMeshes[0];
        return GetMeshData(mesh);
    }
    
    
    private unsafe MeshData GetMeshData(AssimpMesh* mesh, float scaleFactor = 1)
    {

        for (uint i = 0; i < mesh->MNumVertices; i++)
        {
            var vertex = new Vertex3D();

            vertex.Position = mesh->MVertices[i];

            // normals
            if (mesh->MNormals != null)
                vertex.Normal = mesh->MNormals[i];
            // tangent
            if (mesh->MTangents != null)
                vertex.Tangent = mesh->MTangents[i];
            // bitangent
            if (mesh->MBitangents != null)
                vertex.Bitangent = mesh->MBitangents[i];

            // texture coordinates
            if (mesh->MTextureCoords[0] != null)
            {
                Vector3 texcoord3 = mesh->MTextureCoords[0][i];
                vertex.TexCoords = new Vector2(texcoord3.X, texcoord3.Y);
            }

            _vertices.Add(vertex);
        }
        
        for (uint i = 0; i < mesh->MNumFaces; i++)
        {
            Face face = mesh->MFaces[i];
            // retrieve all indices of the face and store them in the indices vector
            for (uint j = 0; j < face.MNumIndices; j++)
                _indices.Add(face.MIndices[j]);
        }

        _verticesBuffer = _vertices.ToArray();
        _indicesBuffer = _indices.ToArray();
        
        return new MeshData
        {
            Vertices = _verticesBuffer,
            Indices = _indicesBuffer,
        };
    }

}