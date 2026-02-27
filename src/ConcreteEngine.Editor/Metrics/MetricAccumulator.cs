using ConcreteEngine.Core.Diagnostics.Metrics;

namespace ConcreteEngine.Editor.Metrics;


internal sealed class MetricAccumulator(int frameRate)
{
    public double CurrentAccTimeMs;

    private double _accTimeMs;
    private double _minMs = float.MaxValue;
    private double _maxMs = float.MinValue;
    private double _avgMs = 0;

    private int _frameCount = 0;

    public bool Accumulate(double frameMs, double spikeMultiplier, out FrameMetrics metrics)
    {
        _accTimeMs += frameMs;
        _frameCount++;

        if (frameMs < _minMs) _minMs = frameMs;
        if (frameMs > _maxMs) _maxMs = frameMs;

        if (_frameCount < frameRate)
        {
            metrics = default;
            return false;
        }

        _avgMs = _accTimeMs / _frameCount;

        CurrentAccTimeMs = _accTimeMs;
        var hasSpike = _maxMs > _avgMs * spikeMultiplier;
        metrics = new FrameMetrics((float)_avgMs, (float)_minMs, (float)_maxMs, hasSpike);

        _accTimeMs = 0;
        _frameCount = 0;
        _minMs = float.MaxValue;
        _maxMs = float.MinValue;

        return true;
    }
}