using ConcreteEngine.Core.Resources;
using ConcreteEngine.Graphics;
using ConcreteEngine.Graphics.Descriptors;
using ConcreteEngine.Graphics.Primitives;
using ConcreteEngine.Graphics.Resources;

namespace ConcreteEngine.Core.Assets;

public sealed class MeshLoader : IAssetTypeLoader, IGpuLazyMeshPayloadProvider
{
    private readonly IReadOnlyList<MeshManifestRecord> _records;
    private readonly List<Mesh> _results = new(16);
    
    private readonly MeshImporter _meshImporter = new();
    private int _idx = 0;

    public bool HasStarted { get; private set; }
    public bool IsFinished =>  _idx >= _records.Count;
    
    internal IReadOnlyList<Mesh> Results => _results;

    public MeshLoader(IReadOnlyList<MeshManifestRecord> records)
    {
        _records = records;
    }

    public void ClearCache()
    {
        _results.Clear();
        _results.TrimExcess();
        _meshImporter.ClearCache();
    }


    public bool TryGet(out int queueIndex, out GpuMeshPayload payload)
    {
        HasStarted = true;
        if (_idx >= _records.Count)
        {
            queueIndex = -1;
            payload = null;
            return false;
        }
        
        var record = _records[_idx];
        var path = Path.Combine(AssetPaths.AssetPath, "meshes", record.Filename);

        var result = _meshImporter.ImportMesh(path);
        var metaDesc = new GpuMeshDescriptor
        {
            VertexPointers =
            [
                VertexAttributeDescriptor.Make<Vertex3D>(nameof(Vertex3D.Position), VertexElementFormat.Float3),
                VertexAttributeDescriptor.Make<Vertex3D>(nameof(Vertex3D.TexCoords), VertexElementFormat.Float2),
                VertexAttributeDescriptor.Make<Vertex3D>(nameof(Vertex3D.Normal), VertexElementFormat.Float3),
                VertexAttributeDescriptor.Make<Vertex3D>(nameof(Vertex3D.Tangent), VertexElementFormat.Float3),
            ],
            DrawKind = MeshDrawKind.Elements,
            DrawCount = (uint)result.Indices.Length
        };

        payload = new GpuMeshPayload(result, metaDesc);
        queueIndex = _idx++;
        return true;
    }

    public void Callback(int queueIndex, in (MeshId, MeshMeta) result)
    {
        var record = _records[queueIndex];
        var (id, meta) = result;

        var mesh = new Mesh
        {
            Name = record.Name,
            Filename = record.Filename,
            IsStatic = meta.IsStatic,
            DrawCount = meta.DrawCount,
            ResourceId = id
        };

        _results.Add(mesh);
    }
}