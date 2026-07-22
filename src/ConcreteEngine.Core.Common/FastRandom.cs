using System.Numerics;
using System.Runtime.CompilerServices;

namespace ConcreteEngine.Core.Common;

public struct FastRandom(uint seed)
{
    private uint _seed = seed == 0 ? 420_1337 : seed;
    
    public void SetSeed(uint seed) => _seed = seed;
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void IncrementSeed() => _seed++;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public float RandomFloat(float min, float max) => min + NextFloat() * (max - min);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public float RandomFloat(Vector2 minMax) => minMax.X + NextFloat() * (minMax.Y - minMax.X);

    [SkipLocalsInit, MethodImpl(MethodImplOptions.AggressiveInlining)]
    public Vector3 NextVector3(float min, float max)
    {
        Vector3 result;
        result.X = RandomFloat(min, max);
        result.Y = RandomFloat(min, max);
        result.Z = RandomFloat(min, max);
        return result;
    }

    // Xorshift algorithm
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public float NextFloat()
    {
        var x = _seed;
        x ^= x << 13;
        x ^= x >> 17;
        x ^= x << 5;
        _seed = x;

        return (x & 0x7FFFFFFF) / (float)int.MaxValue;
    }
}