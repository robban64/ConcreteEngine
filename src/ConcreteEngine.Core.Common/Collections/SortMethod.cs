using System;
using System.Runtime.CompilerServices;

namespace ConcreteEngine.Core.Common.Collections;

public static class SortMethod
{
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static int BinarySearch<T>(ReadOnlySpan<T> collection, T value) where T : unmanaged, IComparable<T>
    {
        int lo = 0, hi = collection.Length - 1;
        while (lo <= hi)
        {
            var mid = lo + ((hi - lo) >> 1);
            var cmp = collection[mid].CompareTo(value);
            if (cmp == 0) return mid;
            if (cmp < 0) lo = mid + 1;
            else hi = mid - 1;
        }

        return -1;
    }


    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static int BinarySearchBy<TClass, TValue>(ReadOnlySpan<TClass?> collection, TValue value, out TClass result)
        where TClass : IComparable<TValue>?
    {
        var lo = 0;
        var hi = collection.Length - 1;

        while (lo <= hi)
        {
            var mid = lo + ((hi - lo) >> 1);
            var it = collection[mid];
            var cmp = it?.CompareTo(value) ?? -1;
            if (cmp == 0)
            {
                result = it!;
                return mid;
            }

            if (cmp < 0) lo = mid + 1;
            else hi = mid - 1;
        }

        result = default!;
        return -1;
    }
}