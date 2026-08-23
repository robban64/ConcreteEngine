namespace ConcreteEngine.Core.Diagnostics.Metrics;

public readonly struct StoreSample(int count, int capacity, int active, int free)
{
    public readonly int Count = count;
    public readonly int Capacity = capacity;
    public readonly int Active = active;
    public readonly int Free = free;
}