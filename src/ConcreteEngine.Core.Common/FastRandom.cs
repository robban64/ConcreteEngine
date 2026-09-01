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

public static class FastRandomExtensions
{
    extension(ref FastRandom rng)
    {
        [SkipLocalsInit, MethodImpl(MethodImplOptions.AggressiveInlining)]
        public Vector3 NextVector3(float min, float max)
        {
            Vector3 result;
            result.X = rng.RandomFloat(min, max);
            result.Y = rng.RandomFloat(min, max);
            result.Z = rng.RandomFloat(min, max);
            return result;
        }

        [SkipLocalsInit, MethodImpl(MethodImplOptions.AggressiveInlining)]
        public Vector4 NextVector3As4(float min, float max)
        {
            Vector4 result = default;
            result.X = rng.RandomFloat(min, max);
            result.Y = rng.RandomFloat(min, max);
            result.Z = rng.RandomFloat(min, max);
            return result;
        }

    }
}