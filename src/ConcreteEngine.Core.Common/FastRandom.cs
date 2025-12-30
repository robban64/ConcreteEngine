using System.Numerics;
using System.Runtime.CompilerServices;

namespace ConcreteEngine.Core.Common;

public struct FastRandom(uint seed)
{
    private uint _state = seed == 0 ? 420_1337 : seed;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public float RandomFloat(float min, float max) => min + NextFloat() * (max - min);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public float RandomFloat(Vector2 minMax) => minMax.X + NextFloat() * (minMax.Y - minMax.X);


    // Xorshift algorithm
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public float NextFloat()
    {
        var x = _state;
        x ^= x << 13;
        x ^= x >> 17;
        x ^= x << 5;
        _state = x;

        return (x & 0x7FFFFFFF) / (float)int.MaxValue;
    }
}