#region

using System.Runtime.CompilerServices;

#endregion

namespace ConcreteEngine.Common.Collections;

public static class SortMethod
{
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static int BinarySearchInt(ReadOnlySpan<int> collection, int value)
    {
        int lo = 0;
        int hi = collection.Length - 1;

        while (lo <= hi)
        {
            int mid = lo + ((hi - lo) >> 1);
            int cmp = value.CompareTo(collection[mid]);

            if (cmp == 0) return mid;
            if (cmp < 0) lo = mid + 1;
            else hi = mid - 1;
        }

        return -1;
    }

    public static int BinarySearch<TObj>(List<TObj> collection, int value) where TObj : IComparable<int>
    {
        int lo = 0;
        int hi = collection.Count - 1;

        while (lo <= hi)
        {
            int mid = lo + ((hi - lo) >> 1);
            int cmp = collection[mid].CompareTo(value);

            if (cmp == 0) return mid;
            if (cmp < 0) lo = mid + 1;
            else hi = mid - 1;
        }

        return -1;
    }
}