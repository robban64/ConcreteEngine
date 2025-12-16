using System.Runtime.CompilerServices;

namespace ConcreteEngine.Engine.Worlds.Entities;

internal static class EntityUtility
{
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static int BinarySearchEntity(ReadOnlySpan<EntityHandle> collection, EntityHandle entity)
    {
        int lo = 0, hi = collection.Length - 1;
        while (lo <= hi)
        {
            int mid = lo + ((hi - lo) >> 1);
            int cmp = collection[mid].CompareTo(entity);
            if (cmp == 0) return mid;
            if (cmp < 0) lo = mid + 1;
            else hi = mid - 1;
        }

        return -1;
    }
}