namespace ConcreteEngine.Common.Collections;

public static class SortMethod
{
    public static int BinarySearchDataInt<TData>(ReadOnlySpan<TData> collection, int value)
        where TData : unmanaged, IComparable<int>
    {
        int lo = 0;
        int hi = collection.Length - 1;

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

    public static int BinarySearchInt<TObj>(List<TObj> collection, int value) where TObj : IComparable<int>
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