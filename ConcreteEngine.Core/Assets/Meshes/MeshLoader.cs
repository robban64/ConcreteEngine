#region

using System.Numerics;
using ConcreteEngine.Core.Assets.Data;
using ConcreteEngine.Graphics.Gfx.Contracts;
using ConcreteEngine.Graphics.Gfx.Definitions;
using ConcreteEngine.Graphics.Gfx.Utility;
using ConcreteEngine.Graphics.Primitives;

#endregion

namespace ConcreteEngine.Core.Assets.Meshes;

internal sealed class MeshLoader
{
    private static VertexAttributeDesc[] DefaultAttribs { get; set; } = Array.Empty<VertexAttributeDesc>();

    private readonly MeshImporter _meshImporter = new();

    public MeshLoader()
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


    public MeshResultPayload LoadMesh(MeshManifestRecord record)
    {
        var path = Path.Combine(AssetPaths.GetAssetPath(), "meshes", record.Filename);

        var fi = new FileInfo(path);
        if (!fi.Exists) throw new FileNotFoundException("File not found.", path);
        var (vertices, indices) = _meshImporter.ImportMesh(path);


        return new MeshResultPayload(
            Attributes: DefaultAttribs,
            Vertices: vertices,
            Indices: indices,
            FileSpec: new AssetFileSpec(
                storage: AssetStorageKind.FileSystem,
                logicalName: record.Name,
                relativePath: record.Filename,
                sizeBytes: fi.Length
            ),
            Properties: new MeshDrawProperties(
                Kind: DrawMeshKind.Elements,
                DrawCount: indices.Count,
                ElementSize: DrawElementSize.UnsignedInt,
                Primitive: DrawPrimitive.Triangles
            )
        );
    }

    public void ClearCache()
    {
        _meshImporter.ClearCache();
    }
}