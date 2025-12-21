using System.Diagnostics;
using System.Text;
using ConcreteEngine.Common;
using ConcreteEngine.Common.Numerics;
using ConcreteEngine.Editor.Utils;

namespace ConcreteEngine.Engine.Time;

internal readonly struct FrameMetricSample(
    double lastAvgMs,
    double maxFrameMs,
    double minFrameMs,
    double load,
    long allocatedBytes,
    byte gcDelta,
    bool hasSpiked)
{
    public readonly float LastAvgMs = (float)lastAvgMs;
    public readonly float MaxFrameMs = (float)maxFrameMs;
    public readonly float MinFrameMs = (float)minFrameMs;
    public readonly int AllocatedMb = (int)(allocatedBytes / 1024 / 1024);
    public readonly Half Load = (Half)load;
    public readonly byte GcCounts = gcDelta;
    public readonly bool HasSpiked = hasSpiked;
}

internal sealed class EngineSystemProfiler
{
    private static EngineSystemProfiler _instance = null!;
    private static readonly StringBuilder Sb = new(256);

    private readonly int _framesPerReport;
    private readonly double _targetFrameMs;
    private readonly Action<FrameMetricSample>? _onSample;

    private readonly Stopwatch _sw = new();
    private readonly List<FrameMetricSample> _history = new(256);

    private long _lastReportFrameIndex = -1;
    private long _lastThreadAllocBytes;
    private GcSample _lastGc;

    private double _accTimeMs;
    private double _minFrameMs = double.MaxValue;
    private double _maxFrameMs = double.MinValue;
    private int _framesInWindow;


    public EngineSystemProfiler(int framesPerReport, double targetFps, Action<FrameMetricSample>? onSample = null)
    {
        if (_instance != null)
            throw new InvalidOperationException("EngineSystemProfiler instance already exists.");

        _instance = this;
        _framesPerReport = framesPerReport;
        _targetFrameMs = 1000.0 / targetFps;
        _onSample = onSample;

        _lastGc = GetCurrentGcSample();
        _lastThreadAllocBytes = GC.GetAllocatedBytesForCurrentThread();
    }

    public void Tick()
    {
        if (!_sw.IsRunning)
        {
            _sw.Start();
            _lastReportFrameIndex = EngineTime.FrameIndex;
            return;
        }

        var rawTicks = _sw.ElapsedTicks;
        _sw.Restart();

        var frameMs = (double)rawTicks / Stopwatch.Frequency * 1000.0;

        _accTimeMs += frameMs;
        if (frameMs < _minFrameMs) _minFrameMs = frameMs;
        if (frameMs > _maxFrameMs) _maxFrameMs = frameMs;
        _framesInWindow++;

        if (EngineTime.FrameIndex >= _lastReportFrameIndex + _framesPerReport)
        {
            PrintAndReset();
            _lastReportFrameIndex = EngineTime.FrameIndex;
        }
    }

    private void PrintAndReset()
    {
        var avgMs = _accTimeMs / _framesInWindow;
        var budgetUsage = (avgMs / _targetFrameMs) * 100.0;
        var fps = 1000.0 / avgMs;
        var isSpike = _maxFrameMs > (avgMs * 2.0);

        var currGc = GetCurrentGcSample();
        var totalGcDelta = GcSample.GetDelta(currGc, _lastGc, out var deltaGc);

        var currAllocBytes = GC.GetAllocatedBytesForCurrentThread();
        var deltaAllocBytes = currAllocBytes - _lastThreadAllocBytes;
        var memKb = deltaAllocBytes / 1024.0;

        var gcByte = (byte)Math.Min(totalGcDelta, 255);

        var sample = new FrameMetricSample(
            avgMs, _minFrameMs, _maxFrameMs, budgetUsage,
            deltaAllocBytes, gcByte, isSpike
        );

        _history.Add(sample);
        _onSample?.Invoke(sample);

        Sb.Clear();

        Span<char> buffer = stackalloc char[16];
        var fsn = new NumberSpanFormatter(buffer);

        var prefix = isSpike ? "[SPIKE]" : "[Frame]";
        Sb.Append(
            $"{prefix} SYS: {fsn.Format(avgMs, "F4")}ms (Min:{fsn.Format(_minFrameMs, "F2")} Max:{fsn.Format(_maxFrameMs, "F2")}) | ");
        Sb.Append($"Load: {fsn.Format(budgetUsage, "F1")}% | ");
        Sb.Append($"FPS: {fsn.Format(fps, "F1")} | ");
        Sb.Append($"Mem: {fsn.Format(memKb, "F1")} KB");


        if (totalGcDelta > 0)
        {
            Sb.Append($" [GC: {fsn.Format(deltaGc.Gen0)}/{fsn.Format(deltaGc.Gen1)}/{fsn.Format(deltaGc.Gen2)}]");
        }

        Console.WriteLine(Sb.ToString());

        _accTimeMs = 0;
        _minFrameMs = double.MaxValue;
        _maxFrameMs = double.MinValue;
        _framesInWindow = 0;

        _lastGc = currGc;
        _lastThreadAllocBytes = currAllocBytes;
    }

    private static GcSample GetCurrentGcSample() =>
        new(GC.CollectionCount(0), GC.CollectionCount(1), GC.CollectionCount(2));

    private readonly struct GcSample(int gen0, int gen1, int gen2)
    {
        public readonly int Gen0 = gen0;
        public readonly int Gen1 = gen1;
        public readonly int Gen2 = gen2;

        public static int GetDelta(GcSample current, GcSample last, out GcSample delta)
        {
            delta = new GcSample(current.Gen0 - last.Gen0, current.Gen1 - last.Gen1, current.Gen2 - last.Gen2);
            return delta.Gen0 + delta.Gen1 + delta.Gen2;
        }
    }
}