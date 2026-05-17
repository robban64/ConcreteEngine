namespace ConcreteEngine.Core.Common.Collections;

public static class SlotHelper
{
    public static int NextSlot(Stack<int> free, int count)
    {
        while (free.TryPeek(out var stale) && stale >= count)
            free.Pop();
        
        return free.TryPop(out var index) ? index : -1;
    }
    
    public static int FreeSlot(Stack<int> free, int index, int count)
    {
        ArgumentOutOfRangeException.ThrowIfGreaterThanOrEqual((uint)index, (uint)count, nameof(index));

        if (index == count - 1) count--;
        else free.Push(index);

        if (count - free.Count == 0)
        {
            free.Clear();
            count = 0;
        }

        return count;
    }


    public static int FreeSlotAndClearStale<T>(Stack<int> free, int index, int count, Span<T> span)
    {
        ArgumentOutOfRangeException.ThrowIfGreaterThanOrEqual((uint)index, (uint)count, nameof(index));
        ArgumentOutOfRangeException.ThrowIfGreaterThanOrEqual((uint)index, (uint)span.Length, nameof(index));

        span[index] = default!;

        if (index == count - 1)
        {
            count--;
            var comparer = EqualityComparer<T>.Default;
            while (count > 0 && comparer.Equals(span[count - 1], default)) count--;
        }
        else
        {
            free.Push(index);
        }

        if (count - free.Count == 0)
        {
            free.Clear();
            count = 0;
        }

        return count;
    }

}
