using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace ConcreteEngine.Core.Common.Collections;

public static class ListExtensions
{
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static int BinarySearch<T>(this List<T> list, T value) where T : IComparable<T>
    {
        return SearchMethod.BinarySearch(CollectionsMarshal.AsSpan(list), value);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static int TryGetBinarySearch<TClass, TValue>(this List<TClass?> list, TValue value,
        out TClass result) where TClass : class, IComparable<TValue>
    {
        return SearchMethod.TryGetBinarySearch(CollectionsMarshal.AsSpan(list), value, out result);
    }
    public static bool TryAddUniqueSorted<T>(this List<T> list, T item) where T : IComparable<T>
    {
        if (list.Count == 0)
        {
            list.Add(item);
            return true;
        }

        var tail = list[^1];
        var cmp = item.CompareTo(tail);

        if (cmp == 0) return false;
        if (cmp > 0)
        {
            list.Add(item);
            return true;
        }

        //
        var index = BinarySearch(list, item);
        if(index >= 0) return false;
        
        list.Add(item);
        list.Sort();
        return true;
    }
}