#region

using System.Numerics;
using ConcreteEngine.Core.Assets.Importers;
using ConcreteEngine.Core.Assets.IO;
using ConcreteEngine.Core.Assets.Manifest;
using ConcreteEngine.Core.Assets.Resources;
using ConcreteEngine.Graphics;
using ConcreteEngine.Graphics.Gfx.Contracts;
using ConcreteEngine.Graphics.Gfx.Definitions;
using ConcreteEngine.Graphics.Gfx.Utility;
using ConcreteEngine.Graphics.Primitives;

#endregion

namespace ConcreteEngine.Core.Assets.Loaders;

internal sealed record MeshResultPayload
{
    public required MeshDrawProperties Properties { get; init; }
    public required List<uint> Indices { get; init; }
    public required List<Vertex3D> Vertices { get; init; }
    public required IReadOnlyList<VertexAttributeDesc> Attributes { get; init; }
}

internal sealed class MeshLoader : AssetTypeLoader<MeshManifestRecord, MeshResultPayload>
{
    private static VertexAttributeDesc[] DefaultAttribs { get; set; } = Array.Empty<VertexAttributeDesc>();

    private readonly List<Mesh> _results = new(16);

    private readonly MeshImporter _meshImporter = new();

    public MeshLoader(IReadOnlyList<MeshManifestRecord> records) : base(records)
    {
        if (DefaultAttribs.Length == 0)
        {
            var attribBuilder = new VertexAttributeMaker<Vertex3D>();
            DefaultAttribs =
            [
                attribBuilder.Make<Vector3>(),
                attribBuilder.Make<Vector2>(),
                attribBuilder.Make<Vector3>(),
                attribBuilder.Make<Vector3>()
            ];
        }
    }


    public override MeshResultPayload ProcessResource(MeshManifestRecord record, out AssetProcessInfo info)
    {
        var path = Path.Combine(AssetPaths.GetAssetPath(), "meshes", record.Filename);

        var (vertices, indices) = _meshImporter.ImportMesh(path);

        info = AssetProcessInfo.MakeDone<MeshManifestRecord>();
        return new MeshResultPayload
        {
            Attributes = DefaultAttribs,
            Vertices = vertices,
            Indices = indices,
            Properties = new MeshDrawProperties
            {
                Kind = DrawMeshKind.Elements,
                DrawCount = indices.Count,
                ElementSize = DrawElementSize.UnsignedInt,
                Primitive = DrawPrimitive.Triangles
            }
        };
    }

    protected override void ClearCache()
    {
        _results.Clear();
        _results.TrimExcess();
        _meshImporter.ClearCache();
    }
}