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
    private const int TerrainHeight = 12;

    private const int SampleSpacing = 1;
    private const float InvSampleSpacing = 1f / SampleSpacing;

    private const int ChunkQuads = TerrainChunk.ChunkQuads;
    private const int ChunkSamples = TerrainChunk.ChunkSamples;

    private Dictionary<Vector2I, TerrainChunk> _chunks;

    public MeshId MeshId { get; internal set; }
    public Material? Material { get; private set; }
    public Texture? Heightmap { get; private set; }

    public bool IsDirty { get; private set; }
    public int MaxHeight { get; private set; } = TerrainHeight;
    public int Dimension { get; private set; }
    public int Size { get; private set; }


    internal TerrainNew()
    {
    }

    public bool HasHeightmap => _chunks.Count > 0 && Heightmap != null;

    public MaterialId MaterialId => Material?.MaterialId ?? MaterialId.Empty;
    public void SetMaterial(Material material) => Material = material;

    public void CreateFrom(Texture heightmap)
    {
        if (!heightmap.PixelData.HasValue)
            throw new ArgumentNullException(nameof(heightmap));

        ArgumentNullException.ThrowIfNull(heightmap);
        ArgumentOutOfRangeException.ThrowIfEqual(heightmap.PixelData.HasValue, false, nameof(heightmap));
        ArgumentOutOfRangeException.ThrowIfLessThan(heightmap.Size.Width, 64, nameof(heightmap));
        ArgumentOutOfRangeException.ThrowIfNotEqual(heightmap.Size.Width, heightmap.Size.Height, nameof(heightmap));

        Heightmap = heightmap;
        Dimension = heightmap.Size.Width;
        Size = heightmap.Size.Width * heightmap.Size.Width;

        CreateTerrainChunks(heightmap.PixelData.Value.Span);

        IsDirty = true;
    }

    private void CreateTerrainChunks(ReadOnlySpan<byte> data)
    {
        int chunkCount = (Dimension - 1) / ChunkQuads;
        for (int z = 0; z < chunkCount; z++)
        {
            //int startZ = z * ChunkQuads;
            for (int x = 0; x < chunkCount; x++)
            {
                //int startX = x * ChunkQuads;
                var coords = new Vector2I(x, z);
                var chunk = new TerrainChunk(coords);
                _chunks.Add(coords, chunk);

                FillChunkHeights(chunk, data);
            }
        }
    }

    private void FillChunkHeights(TerrainChunk chunk, ReadOnlySpan<byte> data)
    {
        var start = chunk.HeightmapStart;
        var heights = chunk.Heights;
        // might help JIT with bound checks
        if (heights.Length < ChunkSamples * ChunkSamples)
            throw new InvalidOperationException("Height map length is less than chunk samples");

        for (int z = 0; z < ChunkSamples; z++)
        {
            for (int x = 0; x < ChunkSamples; x++)
            {
                var heightCoords = new Vector2I(start.X + x, start.Y + z);
                heights[z * ChunkSamples + x] = SampleHeight(data, heightCoords, Dimension, MaxHeight);
            }
        }
    }

    public TerrainChunk GetChunk(float worldX, float worldZ)
    {
        //var coords = new Vector2I((int)float.Floor(worldX / ChunkQuads), (int)float.Floor(worldZ / ChunkQuads));
        var coords = new Vector2I((int)worldX / ChunkQuads, (int)worldZ / ChunkQuads);
        return _chunks[coords];
    }

    public float GetGlobalHeight(float worldX, float worldZ)
    {
        var chunk = GetChunk(worldX, worldZ);
        
        float clampedX = float.Clamp(worldX, 0, Dimension - 1);
        float clampedZ = float.Clamp(worldZ, 0, Dimension - 1);
        
        int lx = (int)clampedX - chunk.HeightmapStart.X;
        int lz = (int)clampedZ - chunk.HeightmapStart.Y;
        return chunk.GetHeight(lx, lz);
        
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static float SampleHeight(ReadOnlySpan<byte> data, Vector2I coords, int dimension, int maxHeight)
    {
        const int channels = 4;

        coords = Vector2I.Clamp(coords, 0, dimension - 1);

        var rowStrideBytes = data.Length / dimension;

        var idx = coords.Y * rowStrideBytes + coords.X * channels;
        if ((uint)(idx + channels - 1) >= (uint)data.Length) return 0f;

        byte r = data[idx];
        return r / 255f * maxHeight;
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

        if (pointOnPlane.X < 0 || pointOnPlane.Z < 0 || pointOnPlane.X > Dimension || pointOnPlane.Z > Dimension)
            return Vector3.Zero;

        var chunk = GetChunk(pointOnPlane.X, pointOnPlane.Z);

        var terrainHeight = chunk.GetSmoothHeight(pointOnPlane.X - chunk.HeightmapStart.X,
            pointOnPlane.Y - chunk.HeightmapStart.Y);

        if (float.Abs(pointOnPlane.Y - terrainHeight) > TerrainHeight)
            return Vector3.Zero;

        return pointOnPlane with { Y = terrainHeight };
    }
}