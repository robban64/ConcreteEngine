using System.Numerics;
using ConcreteEngine.Core.Common;
using ConcreteEngine.Core.Common.Memory;
using ConcreteEngine.Core.Common.Numerics;
using ConcreteEngine.Core.Engine.Graphics;
using ConcreteEngine.Graphics.Gfx;
using ConcreteEngine.Graphics.Gfx.Contracts;
using ConcreteEngine.Graphics.Gfx.Definitions;
using ConcreteEngine.Graphics.Gfx.Handles;
using ConcreteEngine.Graphics.Gfx.Utility;
using ConcreteEngine.Graphics.Primitives;

namespace ConcreteEngine.Engine.Mesh;

internal sealed class TerrainChunkMesh(int slot) : IDisposable
{
    private const int Capacity = TerrainChunk.ChunkSamples * TerrainChunk.ChunkSamples;
    
    public readonly int Slot = slot;
    
    public MeshId MeshId;
    public VertexBufferId VboId;
    
    public BoundingBox Bounds;
    
    private NativeArray<Vertex3D> _vertices = NativeArray.Allocate<Vertex3D>(Capacity, zeroed: true);
    
    public bool HasNullBuffer => _vertices.IsNull;
    public int BufferLength => _vertices.Length;
    public NativeView<Vertex3D> GetVertices() => _vertices;

    public void Dispose() => _vertices.Dispose();
}

internal sealed class TerrainMesh : MeshGenerator
{
    private const int Step = 1;
    private const int IndicesPerQuad = 6;
    private const int ChunkQuads = TerrainChunk.ChunkQuads; // 64
    private const int ChunkSamples = TerrainChunk.ChunkSamples; // 65
    private const int IndexCount = ChunkQuads * ChunkQuads * IndicesPerQuad;

    public const int ChunkCount = 4 * 4;

    public IndexBufferId IboId { get; private set; }

    private NativeArray<ushort> _indexBuffer;
    private TerrainChunkMesh[] _meshChunks = [];


    internal TerrainMesh(GfxContext gfx) : base(gfx)
    {
    }
    
    internal ReadOnlySpan<TerrainChunkMesh> GetMeshChunks() => _meshChunks;

    public override void Dispose()
    {
        _indexBuffer.Dispose();
        foreach (var it in _meshChunks)
        {
            it.Dispose();
        }
    }

    public void Allocate(ReadOnlySpan<TerrainChunk> chunks, ReadOnlySpan<byte> data, int dimension, int gridSize,
        float maxHeight)
    {
        if (IboId.IsValid()) throw new InvalidOperationException("Already allocated");

        _indexBuffer = NativeArray.Allocate<ushort>(IndexCount);
        FillIndexBuffer(_indexBuffer);
        var iboArgs = CreateIboArgs.MakeDefault();
        IboId = Gfx.Buffers.CreateIndexBuffer(_indexBuffer.AsSpan(), iboArgs.Storage, iboArgs.Access, iboArgs.Length);


        int idx = 0;
        _meshChunks = new TerrainChunkMesh[4 * 4];
        foreach (var it in chunks)
        {
            var meshChunk = _meshChunks[idx] = new TerrainChunkMesh(idx);
            //FillVertices(it, meshChunk, dimension);
            //GenerateNormals(it, meshChunk, data, dimension, maxHeight);
            //CalculateBounds(it, meshChunk);
            GenerateCompleteVerticesAndBounds(it, meshChunk, data, dimension, maxHeight);
            CreateChunkMesh(meshChunk);
            idx++;
        }
    }

    private void CreateChunkMesh(TerrainChunkMesh chunkMesh)
    {
        if (chunkMesh.HasNullBuffer) throw new ArgumentNullException(nameof(chunkMesh));

        var args = CreateVboArgs.MakeDynamic(0);
        var props = MeshDrawProperties.MakeElemental(size: DrawElementSize.UnsignedShort, drawCount: IndexCount);

        var vertices = chunkMesh.GetVertices().AsReadOnlySpan();
        
        var meshId = Gfx.Meshes.CreateEmptyMesh(in props, 1, VertexAttributes.GetVertex3DAttributes());
        var vboId = Gfx.Meshes.CreateAttachVertexBuffer(meshId, vertices, args);
        Gfx.Meshes.AttachIndexBuffer(meshId, IboId);

        chunkMesh.MeshId = meshId;
        chunkMesh.VboId = vboId;
    }

    private static void CalculateBounds(TerrainChunk chunk, TerrainChunkMesh mesh)
    {
        if (mesh.HasNullBuffer) throw new ArgumentNullException(nameof(mesh));
        ArgumentOutOfRangeException.ThrowIfLessThan(mesh.BufferLength, ChunkSamples * ChunkSamples);
        var start = chunk.WorldStart;
        var end = chunk.WorldStart + ChunkQuads;

        float minY = float.MaxValue, maxY = float.MinValue;
        for (int z = 0; z < ChunkSamples; z++)
        {
            for (int x = 0; x < ChunkSamples; x++)
            {
                float y = chunk.GetHeight(x, z);
                minY = float.Min(minY, y);
                maxY = float.Max(maxY, y);
            }
        }

        InvalidOpThrower.ThrowIf(minY > maxY);
        mesh.Bounds = new BoundingBox(new Vector3(start.X, minY, start.Y), new Vector3(end.X, maxY, end.Y));
    }

    private static void FillIndexBuffer(NativeArray<ushort> indices)
    {
        if (indices.IsNull) throw new ArgumentNullException(nameof(indices));

        ArgumentOutOfRangeException.ThrowIfLessThan(indices.Length, IndexCount, nameof(indices));
        int i = 0;
        for (int z = 0; z < ChunkQuads; z++)
        {
            for (int x = 0; x < ChunkQuads; x++)
            {
                int bottomLeft = z * ChunkSamples + x;
                int bottomRight = bottomLeft + 1;
                int topLeft = (z + 1) * ChunkSamples + x;
                int topRight = topLeft + 1;

                indices[i++] = (ushort)bottomLeft;
                indices[i++] = (ushort)topLeft;
                indices[i++] = (ushort)bottomRight;

                indices[i++] = (ushort)topLeft;
                indices[i++] = (ushort)topRight;
                indices[i++] = (ushort)bottomRight;
            }
        }
    }

    private static void FillVertices(TerrainChunk chunk, TerrainChunkMesh mesh, int dimension)
    {
        if (mesh.HasNullBuffer) throw new ArgumentNullException(nameof(mesh));

        var vertices = mesh.GetVertices();
        for (int z = 0; z < ChunkSamples; z++)
        {
            for (int x = 0; x < ChunkSamples; x++)
            {
                float worldX = chunk.WorldStart.X + x;
                float worldZ = chunk.WorldStart.Y + z;

                float y = chunk.GetHeight(x, z);

                float u = worldX / (dimension - 1);
                float v = worldZ / (dimension - 1);

                int vi = z * ChunkSamples + x;
                ref var vertex = ref vertices[vi];

                vertex.Position = new Vector3(worldX, y, worldZ);
                vertex.TexCoords = new Vector2(u, v);
            }
        }
    }

    private static void GenerateNormals(TerrainChunk chunk, TerrainChunkMesh mesh, ReadOnlySpan<byte> data,
        int dimension, float maxHeight)
    {
        if (mesh.HasNullBuffer) throw new ArgumentNullException(nameof(mesh));

        var vertices = mesh.GetVertices();

        var start = chunk.WorldStart;
        for (int z = 0; z < ChunkSamples; z++)
        {
            for (int x = 0; x < ChunkSamples; x++)
            {
                var wCoords = start + x;
                var vi = z * ChunkSamples + x;
                
                ref var v = ref vertices[vi];
                v.Normal = TerrainUtils.GetNormal(data, wCoords.X, wCoords.Y, 1, dimension, maxHeight);
                v.Tangent = TerrainUtils.GetTangent(data, wCoords.X, wCoords.Y, 1, dimension, maxHeight, v.Normal);
            }
        }
    }

    //FillVertices, GenerateNormals, CalculateBounds
    private static void GenerateCompleteVerticesAndBounds(TerrainChunk chunk, TerrainChunkMesh mesh,
        ReadOnlySpan<byte> data,
        int dimension, float maxHeight)
    {
        ArgumentNullException.ThrowIfNull(chunk);
        ArgumentNullException.ThrowIfNull(mesh);
        if (mesh.HasNullBuffer) throw new ArgumentNullException(nameof(mesh));

        var vertices = mesh.GetVertices();

        var start = chunk.WorldStart;
        var end = chunk.WorldStart + ChunkQuads;

        float minY = float.MaxValue, maxY = float.MinValue;
        for (int z = 0; z < ChunkSamples; z++)
        {
            for (int x = 0; x < ChunkSamples; x++)
            {
                float worldX = start.X + x;
                float worldZ = start.Y + z;

                float y = chunk.GetHeight(x, z);
                minY = float.Min(minY, y);
                maxY = float.Max(maxY, y);

                float u = worldX / (dimension - 1);
                float v = worldZ / (dimension - 1);

                int vi = z * ChunkSamples + x;

                ref var vx = ref vertices[vi];

                vx.Position = new Vector3(worldX, y, worldZ);
                vx.TexCoords = new Vector2(u, v);

                vx.Normal = TerrainUtils.GetNormal(data, (int)worldX, (int)worldZ, 1, dimension, maxHeight);
                vx.Tangent =
                    TerrainUtils.GetTangent(data, (int)worldX, (int)worldZ, 1, dimension, maxHeight, vx.Normal);
            }
        }
        InvalidOpThrower.ThrowIf(minY > maxY);
        mesh.Bounds = new BoundingBox(new Vector3(start.X, minY, start.Y), new Vector3(end.X, maxY, end.Y));
    }
}