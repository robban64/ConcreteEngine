using ConcreteEngine.Core.Assets.IO;
using ConcreteEngine.Core.Resources;
using ConcreteEngine.Graphics;
using ConcreteEngine.Graphics.Descriptors;
using ConcreteEngine.Graphics.Primitives;

namespace ConcreteEngine.Core.Assets.Loaders;

internal readonly ref struct MeshLoaderResult(GpuMeshData<Vertex3D, uint> meshData, GpuMeshDescriptor descriptor)
{
    public readonly GpuMeshData<Vertex3D, uint> MeshData = meshData;
    public readonly GpuMeshDescriptor Descriptor = descriptor;
}

internal sealed class MeshLoader(IReadOnlyList<MeshManifestRecord> records) : AssetTypeLoader<MeshManifestRecord, MeshLoaderResult>(records)
{
    private readonly List<Mesh> _results = new(16);
    
    private readonly MeshImporter _meshImporter = new();
    
    private static readonly VertexAttributeDescriptor[] Defaults3D =
    [
        VertexAttributeDescriptor.Make<Vertex3D>(nameof(Vertex3D.Position), VertexElementFormat.Float3),
        VertexAttributeDescriptor.Make<Vertex3D>(nameof(Vertex3D.TexCoords), VertexElementFormat.Float2),
        VertexAttributeDescriptor.Make<Vertex3D>(nameof(Vertex3D.Normal), VertexElementFormat.Float3),
        VertexAttributeDescriptor.Make<Vertex3D>(nameof(Vertex3D.Tangent), VertexElementFormat.Float3),
    ];
    
    protected override void ClearCache()
    {
        _results.Clear();
        _results.TrimExcess();
        _meshImporter.ClearCache();
    }


    public override MeshLoaderResult Get(MeshManifestRecord record)
    {
        var path = Path.Combine(AssetPaths.AssetPath, "meshes", record.Filename);

        var meshData = _meshImporter.ImportMesh(path);
        var desc = new GpuMeshDescriptor
        {
            VertexPointers = Defaults3D,
            DrawKind = MeshDrawKind.Elements,
            DrawCount = (uint)meshData.Indices.Length
        };

        return new MeshLoaderResult(meshData, desc);
    }





}