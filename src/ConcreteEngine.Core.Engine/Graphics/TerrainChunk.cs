using System.Numerics;
using System.Runtime.CompilerServices;
using ConcreteEngine.Core.Common.Numerics;

namespace ConcreteEngine.Core.Engine.Graphics;

public sealed class TerrainChunk(Vector2I gridStart)
{
    public const int ChunkQuads = 64;
    public const int ChunkSamples = ChunkQuads + 1;

    public bool IsDirty { get; internal set; }

    public readonly Vector2I WorldStart = gridStart * ChunkQuads;

    private BoundingBox _bounds;

    private readonly float[] _heights = new float[ChunkSamples * ChunkSamples];

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public ref readonly BoundingBox GetBounds() => ref _bounds;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public float GetHeight(int x, int z)
    {
        x = int.Clamp(x, 0, ChunkQuads);
        z = int.Clamp(z, 0, ChunkQuads);
        return _heights[z * ChunkSamples + x];
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal void SetHeight(float height, int x, int z)
    {
        _heights[z * ChunkSamples + x] = height;
        IsDirty = true;
    }

    internal void FillChunkHeights(ReadOnlySpan<byte> heightmap, int dimension, float maxHeight)
    {
        if (_heights.Length < ChunkSamples * ChunkSamples)
            throw new InvalidOperationException("Height map length is less than chunk samples");

        float minY = float.MaxValue, maxY = float.MinValue;

        var start = WorldStart;

        for (int z = 0; z < ChunkSamples; z++)
        {
            for (int x = 0; x < ChunkSamples; x++)
            {
                var heightCoords = new Vector2I(start.X + x, start.Y + z);
                var height = TerrainUtils.SampleHeight(heightmap, heightCoords, dimension, maxHeight);
                minY = float.Min(minY, height);
                maxY = float.Max(maxY, height);
                _heights[z * ChunkSamples + x] = height;
            }
        }

        var end = start + ChunkQuads;
        _bounds = new BoundingBox(new Vector3(start.X, minY, start.Y), new Vector3(end.X, maxY, end.Y));
    }
}