using System.Runtime.CompilerServices;
using ConcreteEngine.Core.Common.Numerics;

namespace ConcreteEngine.Core.Engine.Graphics;

public sealed class TerrainChunk(Vector2I gridStart, float maxHeight)
{
    public const int ChunkQuads = 64;
    public const int ChunkSamples = ChunkQuads + 1;

    public bool IsDirty { get; internal set; }

    public readonly Vector2I GridStart = gridStart;
    public readonly Vector2I WorldStart = gridStart * ChunkQuads;

    internal readonly float[] Heights = new float[ChunkSamples * ChunkSamples];

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal void SetHeight(float height, int x, int y)
    {
        Heights[y * ChunkSamples + x] = height;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public float GetHeight(int x, int y)
    {
        x = int.Clamp(x, 0, ChunkQuads);
        y = int.Clamp(y, 0, ChunkQuads);
        return Heights[y * ChunkSamples + x];
    }
}