using System.Numerics;
using ConcreteEngine.Core.Assets.IO;
using ConcreteEngine.Core.Resources;
using ConcreteEngine.Graphics;
using ConcreteEngine.Graphics.Contracts;
using ConcreteEngine.Graphics.Descriptors;
using ConcreteEngine.Graphics.Primitives;
using ConcreteEngine.Graphics.Utils;

namespace ConcreteEngine.Core.Assets.Loaders;

internal readonly record struct MeshLoaderResult
{
    public required MeshDrawProperties Properties  { get; init; }
    public required List<uint> Indices { get; init; }
    public required List<Vertex3D> Vertices { get; init; }
    public required IReadOnlyList<VertexAttributeDesc> Attributes { get; init; }
}

internal sealed class MeshLoader : AssetTypeLoader<MeshManifestRecord, MeshLoaderResult>
{
    private static VertexAttributeDesc[] DefaultAttribs { get; set; } = Array.Empty<VertexAttributeDesc>();

    private readonly List<Mesh> _results = new(16);
    
    private readonly MeshImporter _meshImporter = new();

    public MeshLoader(IReadOnlyList<MeshManifestRecord> records) : base(records)
    {
        var attribBuilder = new VertexAttributeMaker<Vertex3D>();
        DefaultAttribs = [
           attribBuilder.Make<Vector3>(),
           attribBuilder.Make<Vector2>(),
           attribBuilder.Make<Vector3>(),
           attribBuilder.Make<Vector3>(),
        ];
    }


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
            Attributes = DefaultAttribs,
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