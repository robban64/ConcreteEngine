using System.Runtime.CompilerServices;

namespace ConcreteEngine.Common.Numerics.Maths;

public static class IntMath
{
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool IsPowerOfTwo(int x) => x != 0 && (x & (x - 1)) == 0;
}