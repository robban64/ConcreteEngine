namespace ConcreteEngine.Common.Collections;

public static class Arrays
{
    public const int TableSmallThreshold = 4_096;
    public const int TableDefaultThreshold = 8_192;
    public const int BufferThreshold = 64 * 1024;

    public static int CapacityGrowthSafe(int currentCapacity, int requiredSize,
        int largeThreshold = TableDefaultThreshold)
    {
        if (currentCapacity >= requiredSize) return currentCapacity;

        int newCapacity = currentCapacity == 0 ? 4 : currentCapacity * 2;
        if ((uint)newCapacity < (uint)requiredSize)
        {
            newCapacity = requiredSize;
        }

        if (currentCapacity >= largeThreshold)
        {
            int growth = Math.Max(requiredSize - currentCapacity, largeThreshold);
            newCapacity = currentCapacity + growth;
        }

        return (newCapacity + 63) & ~63;
    }

    public static int CapacityGrowthAlign(int required, int alignment = 4096)
    {
        return (required + (alignment - 1)) & ~(alignment - 1);
    }

    public static int CapacityGrowthToFit(int current, int required)
    {
        var newSize = current;
        while (newSize < required) newSize *= 2;
        return newSize;
    }

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

    public static int CapacityGrowthLinear(int current, int required, int step = 16)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(step);

        var newSize = current;
        while (newSize < required)
            newSize += step;
        return newSize;
    }
}