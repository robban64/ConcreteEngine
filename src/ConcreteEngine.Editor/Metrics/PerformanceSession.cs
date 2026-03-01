namespace ConcreteEngine.Editor.Metrics;
/*
internal sealed class PerformanceSession
{
    public bool HasBaseline { get; private set; }

    public PerformanceMetrics Baseline;
    public PerformanceMetrics Session;

    private int _count;
    private float _accMs;
    private float _accLoad;
    private long _accAlloc;
    private float _sessionMaxAllocRate;
    private float _sessionMin = float.MaxValue;
    private float _sessionMax = float.MinValue;

    public void ClearCurrent()
    {
        _count = 0;
        _accMs = 0;
        _accLoad = 0;
        _accAlloc = 0;

        _sessionMin = float.MaxValue;
        _sessionMax = float.MinValue;
        _sessionMaxAllocRate = 0;

        Session = new PerformanceMetrics();
    }

    public void LoadBaseline()
    {
        InvalidOpThrower.ThrowIf(HasBaseline);
        if (DiagnosticPath.TryLoadPerformanceFile(out Baseline))
            HasBaseline = Baseline is { AvgMs: > 0, AllocatedMb: > 0 };
    }

    public void SaveSession()
    {
        if (_accMs == 0 || _accAlloc == 0) return;
        Baseline = Session;
       // DiagnosticPath.TrySaveSession(in Session);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void Update(in PerformanceMetrics metrics)
    {
        _count++;

        _accMs += metrics.AvgMs;
        _accLoad += metrics.Load;
        _accAlloc += metrics.AllocatedMb;

        if (metrics.MinMs < _sessionMin) _sessionMin = metrics.MinMs;
        if (metrics.MaxMs > _sessionMax) _sessionMax = metrics.MaxMs;

        if (metrics.AllocMbPerSec > _sessionMaxAllocRate)
            _sessionMaxAllocRate = metrics.AllocMbPerSec;

        Session = new PerformanceSnapshot
        {
            AvgMs = _accMs / _count,
            Load = _accLoad / _count,
            AllocatedMb = (int)(_accAlloc / _count),
            MaxAllocRate = _sessionMaxAllocRate,
            MinMs = _sessionMin,
            MaxMs = _sessionMax
        };
    }
}*/