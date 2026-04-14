using System.Numerics;
using System.Runtime.CompilerServices;
using ConcreteEngine.Core.Common.Numerics;
using ConcreteEngine.Core.Common.Numerics.Maths;
using ConcreteEngine.Core.Engine.Assets;
using ConcreteEngine.Core.Renderer;
using ConcreteEngine.Graphics.Gfx.Handles;

namespace ConcreteEngine.Engine.TerrainV2;

public sealed class QuadNode
{
    public int LodLevel;
    public bool IsLeaf;
    public BoundingBox Bounds;
    public QuadNode[] Children;
}


public sealed class TerrainNew
{
    private const int DefaultMaxHeight = 12;

    private const int SampleSpacing = 1;
    private const float InvSampleSpacing = 1f / SampleSpacing;

    private const int ChunkQuads = TerrainChunk.ChunkQuads;
    private const int ChunkSamples = TerrainChunk.ChunkSamples;

    private TerrainChunk[] _chunks = [];

    public Material? Material { get; private set; }
    public Texture? Heightmap { get; private set; }

    public bool IsDirty { get; private set; }
    public float MaxHeight { get; private set; } = DefaultMaxHeight;

    public int Dimension { get; private set; }
    public int SizeSquared { get; private set; }
    public int GridDimension { get; private set; }


    internal TerrainNew()
    {
    }

    public ReadOnlySpan<TerrainChunk> GetChunks() => _chunks;
    public bool HasHeightmap => _chunks.Length > 0 && Heightmap != null;

    public MaterialId MaterialId => Material?.MaterialId ?? MaterialId.Empty;
    public void SetMaterial(Material material) => Material = material;

    public void CreateFrom(Texture heightmap)
    {
        ArgumentNullException.ThrowIfNull(heightmap);

        if (!heightmap.PixelData.HasValue)
            throw new ArgumentNullException(nameof(heightmap));

        ArgumentOutOfRangeException.ThrowIfLessThanOrEqual(heightmap.Size.Width, 128, nameof(heightmap));
        ArgumentOutOfRangeException.ThrowIfNotEqual(heightmap.Size.Width, heightmap.Size.Height, nameof(heightmap));
        var dimension = heightmap.Size.Width;
        
        var powDim = dimension - 1;
        if(!IntMath.IsPowerOfTwo(powDim))
            throw new ArgumentOutOfRangeException(nameof(heightmap.Size), $"Heightmap dimension must be pow2 + 1");

        Heightmap = heightmap;
        Dimension = dimension;
        SizeSquared = dimension * dimension;
        GridDimension = powDim / ChunkQuads;

        _chunks = new TerrainChunk[GridDimension * GridDimension];

        CreateTerrainChunks(heightmap.PixelData.Value.Span);
    }

    public TerrainChunk GetChunk(float worldX, float worldZ)
    {
        int x = int.Clamp((int)worldX / ChunkQuads, 0, GridDimension - 1);
        int z = int.Clamp((int)worldZ / ChunkQuads, 0, GridDimension - 1);
        int idx = z * GridDimension + x;
        return _chunks[idx];
    }

    public float GetGlobalHeight(float worldX, float worldZ)
    {
        float clampedX = float.Clamp(worldX, 0, Dimension - 1);
        float clampedZ = float.Clamp(worldZ, 0, Dimension - 1);
        
        var chunk = GetChunk(clampedX, clampedZ);

        int lx = (int)clampedX - chunk.WorldStart.X;
        int lz = (int)clampedZ - chunk.WorldStart.Y;
        return chunk.GetHeight(lx, lz);
    }
    
    public float GetSmoothHeight(float worldX, float worldZ)
    {
        int ix = (int)worldX;
        int iz = (int)worldZ;
        
        float dx = worldX - ix;
        float dz = worldZ - iz;

        float h00 = GetGlobalHeight(ix, iz);         
        float h10 = GetGlobalHeight(ix + 1, iz);     
        float h01 = GetGlobalHeight(ix, iz + 1);     
        float h11 = GetGlobalHeight(ix + 1, iz + 1); 

        if (dx <= 1.0f - dz)
        {
            return VectorMath.BarryCentric(
                new Vector3(0, h00, 0),
                new Vector3(1, h10, 0),
                new Vector3(0, h01, 1),
                new Vector2(dx, dz));
        }

        return VectorMath.BarryCentric(
            new Vector3(1, h10, 0),
            new Vector3(1, h11, 1),
            new Vector3(0, h01, 1),
            new Vector2(dx, dz));
    }

    public Vector3 GetPointOnTerrainPlane(in Ray ray)
    {
        var n = Vector3.UnitY;
        Vector3 p0 = default;

        var numerator = Vector3.Dot(p0 - ray.Position, n);
        var denominator = Vector3.Dot(ray.Direction, n);

        if (float.Abs(denominator) < 1e-6f)
            return Vector3.Zero;

        var t = numerator / denominator;
        if (t < 0) return Vector3.Zero;
        var pointOnPlane = ray.GetPointOnRay(t);

        if (pointOnPlane.X < 0 || pointOnPlane.Z < 0 || pointOnPlane.X >= Dimension - 1 || pointOnPlane.Z >= Dimension - 1)
            return Vector3.Zero;

        var terrainHeight = GetSmoothHeight(pointOnPlane.X, pointOnPlane.Z);

        if (float.Abs(pointOnPlane.Y - terrainHeight) > DefaultMaxHeight)
            return Vector3.Zero;

        return pointOnPlane with { Y = terrainHeight };
    }

    private void CreateTerrainChunks(ReadOnlySpan<byte> data)
    {
        int chunkCount = GridDimension;
        for (int z = 0; z < chunkCount; z++)
        {
            int rowStart = z * chunkCount;
            for (int x = 0; x < chunkCount; x++)
            {
                var chunk = new TerrainChunk(new Vector2I(x, z), MaxHeight);
                _chunks[rowStart + x] = chunk;

                FillChunkHeights(chunk, data);
            }
        }
    }

    private void FillChunkHeights(TerrainChunk chunk, ReadOnlySpan<byte> data)
    {
        var start = chunk.WorldStart;
        var heights = chunk.Heights;
        
        if (heights.Length < ChunkSamples * ChunkSamples)
            throw new InvalidOperationException("Height map length is less than chunk samples");

        for (int z = 0; z < ChunkSamples; z++)
        {
            for (int x = 0; x < ChunkSamples; x++)
            {
                var heightCoords = new Vector2I(start.X + x, start.Y + z);
                heights[z * ChunkSamples + x] = TerrainUtils.SampleHeight(data, heightCoords, Dimension, MaxHeight);
            }
        }
    }
    
}