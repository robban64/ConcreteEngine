using ConcreteEngine.Core.Assets.IO;
using ConcreteEngine.Core.Resources;
using ConcreteEngine.Graphics;
using ConcreteEngine.Graphics.Contracts;
using ConcreteEngine.Graphics.Descriptors;
using ConcreteEngine.Graphics.Primitives;

namespace ConcreteEngine.Core.Assets.Loaders;

internal readonly record struct MeshLoaderResult
{
    public required MeshDrawProperties Properties  { get; init; }
    public required List<uint> Indices { get; init; }
    public required List<Vertex3D> Vertices { get; init; }
    public required IReadOnlyList<VertexAttributeDesc> Attributes { get; init; }
}

internal sealed class MeshLoader(IReadOnlyList<MeshManifestRecord> records) : AssetTypeLoader<MeshManifestRecord, MeshLoaderResult>(records)
{
    private readonly List<Mesh> _results = new(16);
    
    private readonly MeshImporter _meshImporter = new();
    
    private static readonly VertexAttributeDesc[] Defaults3D =
    [
        VertexAttributeDesc.Make<Vertex3D>(nameof(Vertex3D.Position), VertexElementFormat.Float3),
        VertexAttributeDesc.Make<Vertex3D>(nameof(Vertex3D.TexCoords), VertexElementFormat.Float2),
        VertexAttributeDesc.Make<Vertex3D>(nameof(Vertex3D.Normal), VertexElementFormat.Float3),
        VertexAttributeDesc.Make<Vertex3D>(nameof(Vertex3D.Tangent), VertexElementFormat.Float3),
    ];
    
    protected override void ClearCache()
    {
        _results.Clear();
        _results.TrimExcess();
        _meshImporter.ClearCache();
    }


    public override MeshLoaderResult Get(MeshManifestRecord record)
    {
        var path = Path.Combine(AssetPaths.GetAbsolutePath(), "meshes", record.Filename);

        var (vertices, indices) = _meshImporter.ImportMesh(path);
        
        return new MeshLoaderResult
        {
            Attributes = Defaults3D,
            Vertices = vertices,
            Indices = indices,
            Properties = new MeshDrawProperties
            {
                DrawKind = MeshDrawKind.Elements,
                DrawCount = indices.Count,
                ElementSize = DrawElementSize.UnsignedInt,
                Primitive = DrawPrimitive.Triangles
            }
        };
    }





}