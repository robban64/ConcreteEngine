using System.Numerics;
using System.Runtime.CompilerServices;
using ConcreteEngine.Core.Common.Numerics;
using ConcreteEngine.Core.Common.Numerics.Maths;

namespace ConcreteEngine.Engine.TerrainV2;


public sealed class TerrainChunk(Vector2I gridStart)
{
    public const int ChunkQuads = 64;
    public const int ChunkSamples = ChunkQuads + 1;

    public readonly Vector2I GridStart = gridStart;
    public readonly Vector2I HeightmapStart = gridStart * ChunkQuads;

    public readonly float[] Heights = new float[ChunkSamples * ChunkSamples];

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public float GetHeight(int x, int y)
    {
        x = int.Clamp(x, 0, ChunkQuads);
        y = int.Clamp(y, 0, ChunkQuads);
        return Heights[y * ChunkSamples + x];
    }

    public float GetSmoothHeight(float x, float z)
    {
        var ix = int.Clamp((int)x, 0, ChunkQuads);
        var iz = int.Clamp((int)z, 0, ChunkQuads);

        float gridSquareSize = ChunkSamples / ((float)ChunkSamples * ChunkSamples - 1);
        float xCord = x % gridSquareSize / gridSquareSize;
        float zCord = z % gridSquareSize / gridSquareSize;
        if (xCord <= 1 - zCord)
        {
            return VectorMath.BarryCentric(
                new Vector3(0, GetHeight(ix, iz), 0),
                new Vector3(1, GetHeight(ix + 1, iz), 0),
                new Vector3(0, GetHeight(ix, iz + 1), 1),
                new Vector2(zCord, xCord));
        }

        return VectorMath.BarryCentric(
            new Vector3(1, GetHeight(ix + 1, iz), 0),
            new Vector3(1, GetHeight(ix + 1, iz + 1), 1),
            new Vector3(0, GetHeight(ix, iz + 1), 1),
            new Vector2(xCord, zCord));
    }
}