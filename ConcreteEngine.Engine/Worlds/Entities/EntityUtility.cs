namespace ConcreteEngine.Engine.Worlds.Entities;

internal static class EntityUtility
{
    public static int BinarySearchEntity(ReadOnlySpan<EntityId> collection, EntityId entity)
    {
        var id = entity.Id;

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