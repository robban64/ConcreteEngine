using System.Numerics;
using System.Runtime.CompilerServices;
using ConcreteEngine.Core.Common.Numerics;
using ConcreteEngine.Core.Common.Numerics.Maths;

namespace ConcreteEngine.Engine.TerrainV2;


public sealed class TerrainChunk(Vector2I gridStart, float maxHeight)
{
    public const int ChunkQuads = 64;
    public const int ChunkSamples = ChunkQuads + 1;

    public bool IsDirty {get; internal set;}
    public float MaxHeight = maxHeight;
    
    public readonly Vector2I GridStart = gridStart;
    public readonly Vector2I WorldStart = gridStart * ChunkQuads;

    internal readonly float[] Heights = new float[ChunkSamples * ChunkSamples];


    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void SetHeight(int x, int y, float height)
    {
        x = int.Clamp(x, 0, ChunkQuads);
        y = int.Clamp(y, 0, ChunkQuads);
        Heights[y * ChunkSamples + x] = float.Clamp(height,0, MaxHeight);
        IsDirty = true;
    }


    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public float GetHeight(int x, int y)
    {
        x = int.Clamp(x, 0, ChunkQuads);
        y = int.Clamp(y, 0, ChunkQuads);
        return Heights[y * ChunkSamples + x];
    }

}