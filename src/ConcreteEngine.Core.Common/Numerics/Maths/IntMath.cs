using System.Runtime.CompilerServices;

namespace ConcreteEngine.Core.Common.Numerics.Maths;

public static class IntMath
{

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool IsPowerOfTwo(int x) => x != 0 && (x & (x - 1)) == 0;

    public static int AlignUp(int size, int alignment) => (size + alignment - 1) & ~(alignment - 1);
    public static int AlignDown(int size, int alignment) => size & ~(alignment - 1);

    public static int GetDigits(int n)
    {
        if (n < 10) return 1;
        if (n < 100) return 2;
        if (n < 1000) return 3;
        if (n < 10000) return 4;
        if (n < 100000) return 5;
        if (n < 1000000) return 6;
        if (n < 10000000) return 7;
        if (n < 100000000) return 8;
        if (n < 1000000000) return 9;
        return 10;
    }
}