namespace ConcreteEngine.Core.Common.Collections;

public static class StackUtils
{
    public static int NextSlot(Stack<int> free, int count)
    {
        while (free.TryPeek(out var stale) && stale >= count)
            free.Pop();
        
        return free.TryPop(out var index) ? index : -1;
    }
    
    public static int FreeSlot<T>(Stack<int> free, int index, int count, Span<T> span, T defaultValue) where T : IEquatable<T>
    {
        ArgumentOutOfRangeException.ThrowIfGreaterThanOrEqual((uint)index, (uint)count, nameof(index));
        ArgumentOutOfRangeException.ThrowIfGreaterThanOrEqual((uint)index, (uint)span.Length, nameof(index));

        span[index] = default!;
        
        if (index == count - 1)
        {
            count--;
            while (count > 0 && span[count - 1].Equals(defaultValue)) count--;
        }
        else
        {
            free.Push(index);
        }

        int activeCount = count - free.Count;
        if (activeCount == 0)
        {
            free.Clear();
            count = 0;
        }

        return count;
    }

}


public sealed class FreeStackSlot
{
    private readonly Stack<int> _free = [];
    
    public int FreeCount => _free.Count;
    
    public int NextSlot(int count)
    {
        while (_free.TryPeek(out var stale) && stale >= count)
            _free.Pop();
        
        return _free.TryPop(out var index) ? index : count;
    }

    public int PopSlot<T>(int index, int count, Span<T?> span) where T  : class
    {
        ArgumentOutOfRangeException.ThrowIfGreaterThanOrEqual((uint)index, (uint)count, nameof(index));
        ArgumentOutOfRangeException.ThrowIfGreaterThanOrEqual((uint)index, (uint)span.Length, nameof(index));

        span[index] = null;
        
        if (index == count - 1)
        {
            count--;
            while (count > 0 && span[count - 1] == null) count--;
        }
        else
        {
            _free.Push(index);
        }

        int activeCount = count - _free.Count;
        if (activeCount == 0)
        {
            _free.Clear();
            count = 0;
        }

        return count;
    }
}