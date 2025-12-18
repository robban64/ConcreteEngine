using System.Runtime.CompilerServices;

namespace ConcreteEngine.Engine.ECS.Utility;


internal static class RenderEntityUtility
{
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static int BinarySearchEntity(ReadOnlySpan<RenderEntityId> collection, RenderEntityId renderEntity)
    {
        int lo = 0, hi = collection.Length - 1;
        while (lo <= hi)
        {
            int mid = lo + ((hi - lo) >> 1);
            int cmp = collection[mid].CompareTo(renderEntity);
            if (cmp == 0) return mid;
            if (cmp < 0) lo = mid + 1;
            else hi = mid - 1;
        }

        return -1;
    }
}