namespace ConcreteEngine.Core.Common.Collections;

public static class CapacityUtils
{
    public const int PageSize = 4_096;


    public static int CapacityGrowthToFit(int current, int required)
    {
        var newSize = current;
        while (newSize < required) newSize *= 2;
        return newSize;
    }
/*

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


    public static int CapacityGrowthLinear(int current, int required, int step = 16)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(step);

        var newSize = current;
        while (newSize < required)
            newSize += step;
        return newSize;
    }
    */
}