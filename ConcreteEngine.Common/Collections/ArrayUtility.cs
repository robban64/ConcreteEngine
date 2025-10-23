#region

using System.Runtime.CompilerServices;

#endregion

namespace ConcreteEngine.Common.Collections;

public static class ArrayUtility
{
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static int CapacityGrowthToFit(int current, int required)
    {
        var newSize = current;
        while (newSize < required) newSize *= 2;
        return newSize;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static int CapacityGrowthPow2(int v)
    {
        v--;
        v |= v >> 1;
        v |= v >> 2;
        v |= v >> 4;
        v |= v >> 8;
        v |= v >> 16;
        v++;
        return Math.Max(v, 4);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static int CapacityGrowthLinear(int current, int required, int step = 16)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(step);

        var newSize = current;
        while (newSize < required)
            newSize += step;
        return newSize;
    }
}