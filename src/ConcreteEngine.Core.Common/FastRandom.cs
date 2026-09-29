using System.Numerics;
using System.Runtime.CompilerServices;

namespace ConcreteEngine.Core.Common;

public struct FastRandom(uint seed)
{
    private uint _seed = seed == 0 ? 420_1337 : seed;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void SetSeed(uint seed) => _seed = seed;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void IncrementSeed() => _seed++;
    
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

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public float RandomFloat(float min, float max) => min + NextFloat() * (max - min);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public float RandomFloat(Vector2 minMax) => minMax.X + NextFloat() * (minMax.Y - minMax.X);
    
    [SkipLocalsInit, MethodImpl(MethodImplOptions.AggressiveInlining)]
    public Vector3 RandomVector3(float min, float max)
    {
        var rng = this;
        Vector3 result;
        result.X = rng.RandomFloat(min, max);
        result.Y = rng.RandomFloat(min, max);
        result.Z = rng.RandomFloat(min, max);
        this = rng;
        return result;
    }

}
