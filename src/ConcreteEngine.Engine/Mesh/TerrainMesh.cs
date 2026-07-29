using System.Numerics;
using ConcreteEngine.Core.Common.Memory;
using ConcreteEngine.Core.Common.Numerics.Maths;
using ConcreteEngine.Core.Engine.Graphics.Enviroment;
using ConcreteEngine.Graphics;
using ConcreteEngine.Graphics.Gfx;
using ConcreteEngine.Graphics.Primitives;

namespace ConcreteEngine.Engine.Mesh;

internal sealed class TerrainMesh(GfxContext gfx) : IDisposable
{
    private const int Step = 1;
    private const int IndicesPerQuad = 6;
    private const int ChunkQuads = TerrainChunk.ChunkQuads; // 64
    private const int ChunkSamples = TerrainChunk.ChunkSamples; // 65
    private const int IndexCount = ChunkQuads * ChunkQuads * IndicesPerQuad;

    private const int VertexCapacity = ChunkSamples * ChunkSamples;

    public IndexBufferId TerrainIboId { get; private set; }

    private TerrainChunkMesh[] _meshChunks = [];

    private NativeSoA<Vector3, VertexShading> _vertexBuffer;
    private NativeArray<ushort> _indexBuffer = NativeArray<ushort>.MakeNull();
    private NativeArray<FoliageGpuInstance> _foliageBuffer = NativeArray<FoliageGpuInstance>.MakeNull();

    internal ReadOnlySpan<TerrainChunkMesh> GetMeshChunks() => _meshChunks;

    public int TerrainChunkCount => _meshChunks.Length;
    public int IndexBufferCapacity => _indexBuffer.Length;
    public int VertexBufferCapacity => _vertexBuffer.Length;
    public int FoliageBufferCapacity => _foliageBuffer.Length;
    public bool HasFoliage => _foliageBuffer.Length > 0;

    public void Dispose()
    {
        _indexBuffer.Dispose();
        _vertexBuffer.Dispose();
        _foliageBuffer.Dispose();
    }

    public void Allocate(ReadOnlySpan<TerrainChunk> chunks, ReadOnlySpan<byte> data, int dimension, float maxHeight)
    {
        if (TerrainIboId.IsValid()) throw new InvalidOperationException("Already allocated");

        var vertexLength = IntMath.AlignUp(chunks.Length * VertexCapacity, 4096);

        _indexBuffer = NativeArray.Allocate<ushort>(IndexCount);
        _vertexBuffer = new NativeSoA<Vector3, VertexShading>(vertexLength);

        FillIndexBuffer(_indexBuffer);

        var iboArgs = CreateIboArgs.MakeDefault();
        var terrainIboId = TerrainIboId =
            gfx.Buffers.CreateIndexBuffer(_indexBuffer.AsSpan(), iboArgs.Storage, iboArgs.Access, iboArgs.Length);

        var gfxMeshes = gfx.Meshes;
        _meshChunks = new TerrainChunkMesh[4 * 4];
        for (var i = 0; i < chunks.Length; i++)
        {
            var it = chunks[i];
            var meshChunk = _meshChunks[i] = new TerrainChunkMesh(i);
            meshChunk.GenerateHeightBuffer(it, _vertexBuffer.View1, _vertexBuffer.View2, data, dimension, maxHeight);
            meshChunk.CreateChunkMesh(gfxMeshes, terrainIboId, _vertexBuffer.View1, _vertexBuffer.View2, IndexCount);
        }
    }

    public void AllocateFoliage(Terrain terrain, ReadOnlySpan<byte> data)
    {
        const float density = 2.0f;
        const int maxInstanceCount = (int)(ChunkQuads * density * ChunkQuads * density);

        var chunks = terrain.GetChunks();
        var capacity = IntMath.AlignUp(chunks.Length * maxInstanceCount, 4096);
        _foliageBuffer = NativeArray.Allocate<FoliageGpuInstance>(capacity);

        for (var i = 0; i < chunks.Length; i++)
        {
            var it = chunks[i];
            var meshChunk = _meshChunks[i];
            var view = _foliageBuffer.Slice(maxInstanceCount * i, maxInstanceCount);
            var instanceCount = meshChunk.GenerateFoliageBuffer(terrain, it, view, data, density);
            meshChunk.GenerateFoliageMesh(gfx.Meshes, view, instanceCount);
        }
    }

    private static void FillIndexBuffer(NativeView<ushort> indices)
    {
        if (indices.IsNull) throw new ArgumentNullException(nameof(indices));
        ArgumentOutOfRangeException.ThrowIfLessThan(indices.Length, IndexCount, nameof(indices));

        int i = 0;
        for (int z = 0; z < ChunkQuads; z++)
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