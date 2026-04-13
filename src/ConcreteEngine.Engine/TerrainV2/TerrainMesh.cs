using System.Numerics;
using System.Runtime.CompilerServices;
using ConcreteEngine.Core.Common;
using ConcreteEngine.Core.Common.Memory;
using ConcreteEngine.Core.Common.Numerics;
using ConcreteEngine.Core.Engine.Graphics;
using ConcreteEngine.Engine.Mesh;
using ConcreteEngine.Graphics.Gfx;
using ConcreteEngine.Graphics.Gfx.Contracts;
using ConcreteEngine.Graphics.Gfx.Definitions;
using ConcreteEngine.Graphics.Gfx.Handles;
using ConcreteEngine.Graphics.Gfx.Utility;
using ConcreteEngine.Graphics.Primitives;

namespace ConcreteEngine.Engine.TerrainV2;

internal sealed class TerrainChunkMesh(int slot) : IDisposable
{
    private const int Capacity = TerrainChunk.ChunkSamples * TerrainChunk.ChunkSamples;
    public readonly int Slot = slot;
    public MeshId MeshId;
    public VertexBufferId VboId;
    public BoundingBox Bounds;
    public NativeArray<Vertex3D> Vertices = NativeArray.Allocate<Vertex3D>(Capacity, zeroed: true);

    public void Dispose() => Vertices.Dispose();
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
    public int VertexCount { get; private set; }
    public int DrawCount { get; private set; }

    private NativeArray<ushort> _indexBuffer;

    private TerrainChunkMesh[] MeshChunks = [];

    internal TerrainMesh(GfxContext gfx) : base(gfx)
    {
    }
    public override void Dispose()
    {
        _indexBuffer.Dispose();
        foreach (var it in MeshChunks)
        {
            it.Dispose();
        }
    }

    public void Allocate(Dictionary<Vector2I, TerrainChunk> chunks, ReadOnlySpan<byte> data, int dimension, int maxHeight)
    {
        if (IboId.IsValid()) throw new InvalidOperationException("Already allocated");

        _indexBuffer = NativeArray.Allocate<ushort>(IndexCount);
        FillIndexBuffer(_indexBuffer);
        var iboArgs = CreateIboArgs.MakeDefault();
        IboId = Gfx.Buffers.CreateIndexBuffer(_indexBuffer.AsSpan(), iboArgs.Storage, iboArgs.Access, iboArgs.Length);


        var attribBuilder = new VertexAttributeMaker();
        ReadOnlySpan<VertexAttribute> attributes = stackalloc VertexAttribute[4]
        {
            attribBuilder.Make<Vector3>(0), attribBuilder.Make<Vector2>(1),
            attribBuilder.Make<Vector3>(2), attribBuilder.Make<Vector3>(3)
        };

        int idx = 0;
        MeshChunks = new TerrainChunkMesh[4 * 4];
        foreach (var it in chunks.Values)
        {
            var meshChunk = MeshChunks[idx] = new TerrainChunkMesh(idx);
            FillVertices(it, meshChunk, dimension, maxHeight);
            GenerateNormals(it, meshChunk, data, dimension, maxHeight);
            CalculateBounds(it, meshChunk);
            CreateChunkMesh(meshChunk, attributes);
            idx++;
        }
    }



    private void CreateChunkMesh(TerrainChunkMesh chunkMesh, ReadOnlySpan<VertexAttribute> attributes)
    {
        var props = MeshDrawProperties.MakeElemental(size: DrawElementSize.UnsignedShort, drawCount: IndexCount);

        var args = CreateVboArgs.MakeDynamic(0);

        var meshId = Gfx.Meshes.CreateEmptyMesh(in props, 2, attributes);
        var vboId = Gfx.Meshes.CreateAttachVertexBuffer(meshId, chunkMesh.Vertices.AsSpan(), args);
        Gfx.Meshes.AttachIndexBuffer(meshId, IboId);

        chunkMesh.MeshId = meshId;
        chunkMesh.VboId = vboId;
    }

    private static void CalculateBounds(TerrainChunk chunk, TerrainChunkMesh mesh)
    {
        var start = chunk.WorldStart;

        float minY = float.MaxValue, maxY = float.MinValue;
        for (int z = 0; z < ChunkSamples; z++)
        {
            for (int x = 0; x < ChunkSamples; x++)
            {
                float worldX = chunk.WorldStart.X + x;
                float worldZ = chunk.WorldStart.Y + z;

                float y = chunk.GetHeight(x, z);
                minY = float.Min(minY, y);
                maxY = float.Max(maxY, y);
            }
        }
        InvalidOpThrower.ThrowIf(minY > maxY);
        mesh.Bounds = new BoundingBox(new Vector3(start.X, minY, start.Y), new Vector3(start.X + ChunkQuads, maxY, start.Y + ChunkQuads));

    }

    private static void FillVertices(TerrainChunk chunk, TerrainChunkMesh mesh, int dimension, int maxHeight)
    {
        int vertexCount = ChunkSamples * ChunkSamples;
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
                ref var vertex = ref mesh.Vertices[vi];

                vertex.Position = new Vector3(worldX, y, worldZ);
                vertex.TexCoords = new Vector2(u, v);
            }
        }
    }


    private static void GenerateNormals(TerrainChunk chunk, TerrainChunkMesh mesh, ReadOnlySpan<byte> data,
        int dimension, int maxHeight)
    {
        int vertexCount = ChunkSamples * ChunkSamples;
        for (int z = 0; z < ChunkSamples; z++)
        {
            for (int x = 0; x < ChunkSamples; x++)
            {
                int vi = z * ChunkSamples + x;
                ref var vertex = ref mesh.Vertices[vi];
                vertex.Normal = GetNormal(data, x, z, 1, dimension, maxHeight);
                vertex.Tangent = GetTangent(data, x, z, 1, dimension, maxHeight, vertex.Normal);
            }
        }
    }

    private static void FillIndexBuffer(NativeArray<ushort> indices)
    {
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

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static Vector3 GetTangent(ReadOnlySpan<byte> data, int worldX, int worldZ, int step, int dimension,
        int maxHeight, Vector3 n)
    {
        var hL = TerrainNew.SampleHeight(data, (worldX - step, worldZ), dimension, maxHeight);
        var hR = TerrainNew.SampleHeight(data, (worldX + step, worldZ), dimension, maxHeight);

        var rawT = new Vector3(2 * step, hR - hL, 0f);
        var t = rawT - n * Vector3.Dot(rawT, n);
        return NormalizeSafe(t);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static Vector3 GetNormal(ReadOnlySpan<byte> data, int worldX, int worldZ, int step, int dimension,
        int maxHeight)
    {
        var hL = TerrainNew.SampleHeight(data, (worldX - step, worldZ), dimension, maxHeight);
        var hR = TerrainNew.SampleHeight(data, (worldX + step, worldZ), dimension, maxHeight);
        var hD = TerrainNew.SampleHeight(data, (worldX, worldZ - step), dimension, maxHeight);
        var hU = TerrainNew.SampleHeight(data, (worldX, worldZ + step), dimension, maxHeight);

        var dx = new Vector3(2 * step, hR - hL, 0f);
        var dz = new Vector3(0f, hU - hD, 2 * step);

        return NormalizeSafe(Vector3.Cross(dz, dx));
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static Vector3 NormalizeSafe(Vector3 v)
    {
        var len2 = v.LengthSquared();
        return len2 > 1e-12f ? v / MathF.Sqrt(len2) : Vector3.UnitY;
    }
}