using System.Runtime.CompilerServices;

namespace ConcreteEngine.Core.Common.Numerics.Maths;

public static class IntMath
{
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool IsPowerOfTwo(int x) => x != 0 && (x & (x - 1)) == 0;

    public static int AlignUp(int size, int alignment) => (size + alignment - 1) & ~(alignment - 1);
    public static int AlignDown(int size, int alignment) => size & ~(alignment - 1);

}