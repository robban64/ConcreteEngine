using System.Numerics;
using System.Runtime.CompilerServices;
using ConcreteEngine.Core.Common.Numerics;
using ConcreteEngine.Core.Common.Numerics.Maths;
using ConcreteEngine.Core.Engine.Assets;
using ConcreteEngine.Renderer.Core;

namespace ConcreteEngine.Core.Engine.Graphics;

public sealed class QuadNode
{
    public int LodLevel;
    public bool IsLeaf;
    public BoundingBox Bounds;
    public QuadNode[] Children;
}

public sealed class Terrain
{
    private const int DefaultMaxHeight = 12;

    private const int SampleSpacing = 1;
    private const float InvSampleSpacing = 1f / SampleSpacing;

    private const int ChunkQuads = TerrainChunk.ChunkQuads;
    private const int ChunkSamples = TerrainChunk.ChunkSamples;

    public static Terrain Main { get; internal set; } = null!;

    private TerrainChunk[] _chunks = [];

    public bool IsDirty { get; internal set; } = true;

    public float MaxHeight { get; private set; } = DefaultMaxHeight;
    public int Dimension { get; private set; }
    public int GridDimension { get; private set; }

    public readonly TextureArray GroundAlbedoTextures = new(4);
    public readonly TextureArray GroundNormalTextures = new(4);
    public readonly TextureArray FoliageTextures = new(4);

    public Material? GroundMaterial { get; private set; }
    public Material? FoliageMaterial { get; private set; }

    public Texture? Heightmap { get; private set; }
    public Texture? Splatmap { get; private set; }

    internal Terrain()
    {
    }

    public bool HasHeightmap => _chunks.Length > 0 && Heightmap is { HasPixelData: true };

    public ReadOnlySpan<TerrainChunk> GetChunks() => _chunks;

    public MaterialId MaterialId => GroundMaterial?.MaterialId ?? Material.FallbackMaterial.MaterialId;
    public MaterialId FoliageMaterialId => FoliageMaterial?.MaterialId ?? Material.FallbackMaterial.MaterialId;

    public void SetTexture(int slot, Texture texture)
    {
        ArgumentOutOfRangeException.ThrowIfGreaterThanOrEqual(slot, 4);
        GroundAlbedoTextures.SetTexture(slot, texture);
        IsDirty = true;
    }

    public void SetFoliageTexture(int slot, Texture texture)
    {
        ArgumentOutOfRangeException.ThrowIfGreaterThanOrEqual(slot, 4);
        FoliageTextures.SetTexture(slot, texture);
        IsDirty = true;
    }


    public void SetMaterial(Material material)
    {
        GroundMaterial = material;
        IsDirty = true;
    }

    public void SetFoliageMaterial(Material material)
    {
        FoliageMaterial = material;
        IsDirty = true;
    }

    public void CreateFrom(Texture heightmap, Texture? splatMap = null)
    {
        ArgumentOutOfRangeException.ThrowIfLessThanOrEqual(heightmap.Size.Width, 128, nameof(heightmap));
        ArgumentOutOfRangeException.ThrowIfNotEqual(heightmap.Size.Width, heightmap.Size.Height, nameof(heightmap));

        ArgumentNullException.ThrowIfNull(heightmap);

        var dimension = heightmap.Size.Width;

        var powDim = dimension - 1;
        if (!IntMath.IsPowerOfTwo(powDim))
            throw new ArgumentOutOfRangeException(nameof(heightmap.Size), "Heightmap dimension must be pow2 + 1");

        if (!heightmap.TryGetPixelSpan(out var pixelData)) throw new ArgumentNullException(nameof(heightmap));

        Heightmap = heightmap;
        Dimension = dimension;
        GridDimension = powDim / ChunkQuads;
        Splatmap = splatMap;

        _chunks = new TerrainChunk[GridDimension * GridDimension];

        CreateTerrainChunks(pixelData);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public TerrainChunk GetChunk(int slot) => _chunks[slot];

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public TerrainChunk GetChunk(float worldX, float worldZ)
    {
        int x = int.Clamp((int)worldX / ChunkQuads, 0, GridDimension - 1);
        int z = int.Clamp((int)worldZ / ChunkQuads, 0, GridDimension - 1);
        int idx = z * GridDimension + x;
        return _chunks[idx];
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void SetHeight(float worldX, float worldZ, float height)
    {
        worldX = float.Clamp(worldX, 0, Dimension - 1);
        worldZ = float.Clamp(worldZ, 0, Dimension - 1);

        var chunk = GetChunk(worldX, worldZ);

        int lx = (int)worldX - chunk.WorldStart.X;
        int lz = (int)worldZ - chunk.WorldStart.Y;

        height = float.Clamp(height, 0, MaxHeight);
        chunk.SetHeight(height, lx, lz);

        IsDirty = true;
    }

    public float GetHeight(float worldX, float worldZ)
    {
        worldX = float.Clamp(worldX, 0, Dimension - 1);
        worldZ = float.Clamp(worldZ, 0, Dimension - 1);

        var chunk = GetChunk(worldX, worldZ);

        int lx = (int)worldX - chunk.WorldStart.X;
        int lz = (int)worldZ - chunk.WorldStart.Y;
        return chunk.GetHeight(lx, lz);
    }

    public float GetSmoothHeight(float worldX, float worldZ)
    {
        int ix = (int)worldX;
        int iz = (int)worldZ;

        float dx = worldX - ix;
        float dz = worldZ - iz;

        float h00 = GetHeight(ix, iz);
        float h10 = GetHeight(ix + 1, iz);
        float h01 = GetHeight(ix, iz + 1);
        float h11 = GetHeight(ix + 1, iz + 1);

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

        if (pointOnPlane.X < 0 || pointOnPlane.Z < 0 || pointOnPlane.X >= Dimension - 1 ||
            pointOnPlane.Z >= Dimension - 1)
            return Vector3.Zero;

        var terrainHeight = GetSmoothHeight(pointOnPlane.X, pointOnPlane.Z);

        if (float.Abs(pointOnPlane.Y - terrainHeight) > DefaultMaxHeight)
            return Vector3.Zero;

        return pointOnPlane with { Y = terrainHeight };
    }


    private void CreateTerrainChunks(ReadOnlySpan<byte> heightmap)
    {
        int chunkCount = GridDimension;
        for (int z = 0; z < chunkCount; z++)
        {
            int rowStart = z * chunkCount;
            for (int x = 0; x < chunkCount; x++)
            {
                var chunk = new TerrainChunk(new Vector2I(x, z));
                chunk.FillChunkHeights(heightmap, Dimension, MaxHeight);
                _chunks[rowStart + x] = chunk;
            }
        }
    }
}