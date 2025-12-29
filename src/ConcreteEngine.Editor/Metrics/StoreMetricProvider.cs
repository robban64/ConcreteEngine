using ConcreteEngine.Core.Diagnostics.Metrics;
using ConcreteEngine.Editor.Utils;

namespace ConcreteEngine.Editor.Metrics;

internal sealed class AssetStoreMetricProvider : MetricProvider
{
    private readonly PairSample[] _data;

    internal AssetStoreMetricProvider(int storeCount)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(storeCount);
        _data = new PairSample[storeCount];
    }

    protected override void ClearData() => _data.AsSpan().Clear();
    
    public void Fill(Span<PairSample> span)
    {
        ArgumentOutOfRangeException.ThrowIfNotEqual(_data.Length, span.Length);
        span.CopyTo(_data);
    }
}

internal sealed class GfxStoreMetricProvider : MetricProvider
{
    private readonly GfxStoreMeta[] _data;
    private readonly string[] _formattedMetric;

    public ReadOnlySpan<GfxStoreMeta> Data => _data;
    public ReadOnlySpan<string> FormattedMetric => _formattedMetric;
    

    public GfxStoreMetricProvider(int storeCount)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(storeCount);
        
        _data = new GfxStoreMeta[storeCount];
        _formattedMetric = new  string[storeCount];
    }

    protected override void ClearData()
    {
        _data.AsSpan().Clear();
        _formattedMetric.AsSpan().Clear();
    }

    public void Fill(Span<GfxStoreMeta> span)
    {
        ArgumentOutOfRangeException.ThrowIfNotEqual(_data.Length, span.Length);

        span.CopyTo(_data);
        for (var i = 0; i < span.Length; i++)
            _formattedMetric[i] = MetricsFormatter.FormatGfxStoreMeta(in span[i]);
    }
}
