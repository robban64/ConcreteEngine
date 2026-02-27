namespace ConcreteEngine.Editor.Metrics;
/*
internal abstract class MetricProvider
{
    protected long LastUpdate = -1;

    public bool Enabled { get; private set; }
    public bool HasData => LastUpdate > 0;

    public abstract void ClearData();
    protected virtual void OnToggle(bool toggle) { }

    public void Toggle(bool enabled)
    {
        if (Enabled == enabled) return;
        Enabled = enabled;
        ClearData();
        LastUpdate = -1;
        OnToggle(enabled);
    }
}

internal sealed class StoreMetricProvider<TData>(int storeCount, Action<Span<TData>> onRequestRefresh)
    : MetricProvider where TData : unmanaged
{
    private readonly TData[] _data = new TData[storeCount];
    internal ReadOnlySpan<TData> GetData() => _data;

    internal Action? OnDataChange;

    public override void ClearData() => _data.AsSpan().Clear();

    protected override void OnToggle(bool toggle)
    {
        if (toggle) Refresh();
    }

    internal void Refresh()
    {
        Span<TData> span = stackalloc TData[storeCount];
        onRequestRefresh(span);
        span.CopyTo(_data);
        OnDataChange?.Invoke();

        ConsoleGateway.LogPlain($"Refreshing store metrics: {span.Length}");
    }
}

internal sealed class PollMetricProvider<T>(long intervalTicks, FuncFill<T> onFetch)
    : MetricProvider where T : unmanaged
{
    private long _intervalTicks = intervalTicks;

    public override void ClearData() => MetricsApi.Provider<T>.Data = default;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal void Tick(long ticks)
    {
        if (ticks - LastUpdate > _intervalTicks)
        {
            onFetch(out MetricsApi.Provider<T>.Data);
            LastUpdate = ticks;
        }
    }

    public void SetIntervalTicks(long intervalTicks)
    {
        if (_intervalTicks == intervalTicks) return;
        _intervalTicks = intervalTicks;
        LastUpdate = -1;
    }
}*/