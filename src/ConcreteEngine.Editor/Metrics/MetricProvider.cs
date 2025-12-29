using ConcreteEngine.Core.Common;

namespace ConcreteEngine.Editor.Metrics;

internal abstract class MetricProvider
{
    protected long LastUpdate = -1;

    public bool Enabled { get; private set; }
    public bool HasData => LastUpdate > 0;

    protected abstract void ClearData();
    protected virtual void OnToggle(bool toggle) { }

    internal virtual void Tick(long ticks) { }

    public void Toggle(bool enabled)
    {
        if (Enabled == enabled) return;
        Enabled = enabled;
        ClearData();
        LastUpdate = -1;
        OnToggle(enabled);
    }
}

internal class EventMetricProvider<TData>(int storeCount, Action<Span<TData>> onRequestRefresh)
    : MetricProvider where TData : unmanaged
{
    private readonly TData[] _data = new TData[storeCount];
    internal ReadOnlySpan<TData> Data => _data;


    protected override void ClearData() => _data.AsSpan().Clear();

    protected override void OnToggle(bool toggle)
    {
        if (toggle) Refresh();
    }

    public void Refresh()
    {
        Span<TData> span = stackalloc TData[storeCount];
        onRequestRefresh(span);
        Console.WriteLine("Refreshing metrics for " + span.Length);
        span.CopyTo(_data);
    }
}

internal sealed class PollMetricProvider<T>(long intervalTicks, FuncFill<T> onFetch)
    : MetricProvider where T : unmanaged
{
    private long _intervalTicks = intervalTicks;

    public T Data;

    protected override void ClearData() => Data = default;

    internal override void Tick(long ticks)
    {
        if (ticks - LastUpdate > _intervalTicks)
        {
            onFetch(out Data);
            LastUpdate = ticks;
        }
    }

    public void SetIntervalTicks(long intervalTicks)
    {
        if (_intervalTicks == intervalTicks) return;
        _intervalTicks = intervalTicks;
        LastUpdate = -1;
    }
}