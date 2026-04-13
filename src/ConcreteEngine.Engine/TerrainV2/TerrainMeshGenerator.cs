using System.Numerics;
using System.Runtime.CompilerServices;
using ConcreteEngine.Core.Common.Memory;
using ConcreteEngine.Core.Common.Numerics;
using ConcreteEngine.Core.Engine.Graphics;
using ConcreteEngine.Engine.Mesh;
using ConcreteEngine.Graphics.Gfx;
using ConcreteEngine.Graphics.Gfx.Contracts;
using ConcreteEngine.Graphics.Gfx.Handles;
using ConcreteEngine.Graphics.Gfx.Utility;
using ConcreteEngine.Graphics.Primitives;

namespace ConcreteEngine.Engine.TerrainV2;

internal sealed class TerrainMeshNew : MeshGenerator
{
    private const int Step = 1;
    const int IndicesPerQuad = 6;
    private const int ChunkQuads = TerrainChunk.ChunkQuads; // 64
    private const int ChunkSamples = TerrainChunk.ChunkSamples; // 65

    public MeshId MeshId { get; private set; }

    private NativeArray<ushort> _indexBuffer;
    private NativeArray<Vertex3D> _vertexBuffer;

    public int VertexCount { get; private set; }
    public int DrawCount { get; private set; }

    internal TerrainMeshNew(GfxContext gfx) : base(gfx)
    {
    }

    public void Allocate(int maxChunks)
    {
        _indexBuffer = NativeArray.Allocate<ushort>(ChunkQuads * ChunkQuads * IndicesPerQuad);
        _vertexBuffer = NativeArray.Allocate<Vertex3D>(ChunkSamples * ChunkSamples * maxChunks);
    }

/*
    public MeshId CreateTerrainMesh(Terrain terrain)
    {
        var vertexRowCount = (terrain.Dimension - 1) / terrain.Step + 1;
        VertexCount = vertexRowCount * vertexRowCount;

        GenerateVertex(terrain, vertexRowCount);
        GenerateIndices(vertexRowCount);
        RecomputeNormalsFromIndices();
        GenerateMesh();

        MeshId.IsValidOrThrow();
        return MeshId;
    }
*/

    public override void Dispose()
    {
    }

    public void GenerateChunkVertices(TerrainChunk chunk, ReadOnlySpan<byte> data, int dimension, int maxHeight)
    {
        int vertexCount = ChunkSamples * ChunkSamples;
        var vertices = _vertexBuffer;
        //var chunkHandle = new Range32(N * vertexCount, N * vertexCount + vertexCount);
        for (int z = 0; z < ChunkSamples; z++)
        {
            for (int x = 0; x < ChunkSamples; x++)
            {
                float worldX = chunk.HeightmapStart.X + x;
                float worldZ = chunk.HeightmapStart.Y + z;

                float y = chunk.GetHeight(x, z);

                float u = worldX / (dimension - 1);
                float v = worldZ / (dimension - 1);

                int vi = z * ChunkSamples + x;
                ref var vertex = ref vertices[vi];

                vertex.Position = new Vector3(worldX, y, worldZ);
                vertex.TexCoords = new Vector2(u, v);

                vertex.Normal = GetNormal(data, x, z, 1, dimension, maxHeight);
                vertex.Tangent = GetTangent(data, x, z, 1, dimension, maxHeight, vertex.Normal);
            }
        }
    }

    public static void FillIndexBuffer(NativeArray<ushort> indices)
    {
        const int totalIndices = ChunkSamples * ChunkSamples * IndicesPerQuad;
        ArgumentOutOfRangeException.ThrowIfLessThan(indices.Length, totalIndices, nameof(indices));
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