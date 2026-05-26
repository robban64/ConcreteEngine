using System.Numerics;
using System.Runtime.CompilerServices;
using ConcreteEngine.Core.Common.Numerics;

namespace ConcreteEngine.Core.Engine.Graphics;

public static class TerrainUtils
{
    public static int SampleLayer(ReadOnlySpan<byte> pixelData, int x, int z, int dimension)
    {
        const int channels = 4;
        const byte threshold = 64;
        
        x = int.Clamp(x, 0, dimension - 1);
        z = int.Clamp(z, 0, dimension - 1);

        var rowStrideBytes = pixelData.Length / dimension;

        var idx = z * rowStrideBytes + x * channels;
        if ((uint)(idx + channels - 1) >= (uint)pixelData.Length) return -1;

        byte r = pixelData[idx], g = pixelData[idx + 1], b = pixelData[idx + 2];
        byte w = (byte)((r + g + b) / 3);

        int bestLayer = -1;
        byte bestValue = threshold;

        if (r > bestValue)
        {
            bestValue = r;
            bestLayer = 0;
        }
        if (g > bestValue)
        {
            bestValue = g;
            bestLayer = 1;
        }
        if (b > bestValue)
        {
            bestValue = b;
            bestLayer = 2;
        }
        if (w > bestValue)
        {
            bestValue = w;
            bestLayer = 3;
        }

        return bestLayer;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static float SampleHeight(ReadOnlySpan<byte> data, Vector2I coords, int dimension, float maxHeight)
    {
        const int channels = 4;

        coords = Vector2I.Clamp(coords, 0, dimension - 1);

        var rowStrideBytes = data.Length / dimension;

        var idx = coords.Y * rowStrideBytes + coords.X * channels;
        if ((uint)(idx + channels - 1) >= (uint)data.Length) return 0f;

        byte r = data[idx];
        return r / 255f * maxHeight;
    }
    

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Vector3 GetTangent(ReadOnlySpan<byte> data, int worldX, int worldZ, int step, int dimension,
        float maxHeight, Vector3 n)
    {
        var hL = SampleHeight(data, (worldX - step, worldZ), dimension, maxHeight);
        var hR = SampleHeight(data, (worldX + step, worldZ), dimension, maxHeight);

        var rawT = new Vector3(2 * step, hR - hL, 0f);
        var t = rawT - n * Vector3.Dot(rawT, n);
        return NormalizeSafe(t);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Vector3 GetNormal(ReadOnlySpan<byte> data, int worldX, int worldZ, int step, int dimension,
        float maxHeight)
    {
        var hL = SampleHeight(data, (worldX - step, worldZ), dimension, maxHeight);
        var hR = SampleHeight(data, (worldX + step, worldZ), dimension, maxHeight);
        var hD = SampleHeight(data, (worldX, worldZ - step), dimension, maxHeight);
        var hU = SampleHeight(data, (worldX, worldZ + step), dimension, maxHeight);

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