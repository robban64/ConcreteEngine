using ConcreteEngine.Core.Common;

namespace ConcreteEngine.Editor.Metrics;

internal abstract class MetricProvider(long intervalTicks)
{
    private long _intervalTicks = intervalTicks;
    private long _lastUpdate = -1;

    public bool Enabled { get; protected set; }

    public bool HasData => _lastUpdate > 0;

    protected abstract void ClearData();
    protected abstract void OnUpdate(long currentTicks);

    public void Toggle(bool enabled)
    {
        if (Enabled == enabled) return;
        Enabled = enabled;
        ClearData();
        _lastUpdate = -1;
    }

    public void SetIntervalTicks(long intervalTicks)
    {
        if (_intervalTicks == intervalTicks) return;
        _intervalTicks = intervalTicks;
        _lastUpdate = -1;
    }

    public void Update(long currentTicks)
    {
        if (currentTicks - _lastUpdate > _intervalTicks)
        {
            OnUpdate(currentTicks);
            _lastUpdate = currentTicks;
        }
    }
}

internal sealed class MetricProvider<T>(long intervalTicks, FuncFill<T> onFetch)
    : MetricProvider(intervalTicks) where T : unmanaged
{
    public T Data;
    protected override void ClearData() => Data = default;
    protected override void OnUpdate(long currentTicks) => onFetch(out Data);
}