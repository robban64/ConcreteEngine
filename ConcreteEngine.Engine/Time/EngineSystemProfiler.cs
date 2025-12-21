using System.Diagnostics;
using System.Text;
using ConcreteEngine.Common;
using ConcreteEngine.Common.Numerics;
using ConcreteEngine.Editor.Utils;
using ZaString.Core;
using ZaString.Extensions;

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
    public readonly int AllocatedMb = (int)(allocatedBytes / 1024 / 1024);
    public readonly Half LastAvgMs = (Half)lastAvgMs;
    public readonly Half MaxFrameMs = (Half)maxFrameMs;
    public readonly Half MinFrameMs = (Half)minFrameMs;
    public readonly Half Load = (Half)load;
    public readonly byte GcCounts = gcDelta;
    public readonly bool HasSpiked = hasSpiked;
}

internal sealed class EngineSystemProfiler
{
    private static EngineSystemProfiler _instance = null!;
    private readonly Stopwatch _sw = new();

    private readonly Action<FrameMetricSample>? _onSample;
    private readonly List<FrameMetricSample> _history = new(256);

    private readonly int _framesPerReport;
    private readonly double _targetFrameMs;

    private double _accTimeMs;
    private long _frameIndex = -1;
    private int _framesInWindow;

    private MemoryState _memoryState;
    private TimeState _timeState;

    public EngineSystemProfiler(int framesPerReport, double targetFps, Action<FrameMetricSample>? onSample = null)
    {
        if (_instance != null)
            throw new InvalidOperationException("EngineSystemProfiler instance already exists.");

        _instance = this;
        _framesPerReport = framesPerReport;
        _targetFrameMs = 1000.0 / targetFps;
        _onSample = onSample;

        _timeState.MinMs = double.MaxValue;
        _timeState.MaxMs = double.MinValue;

        _memoryState.LastGcSample = GetCurrentGcSample();
        _memoryState.AllocatedBytes = GC.GetAllocatedBytesForCurrentThread();
    }

    public void Tick()
    {
        if (!_sw.IsRunning)
        {
            _sw.Start();
            _frameIndex = EngineTime.FrameIndex;
            return;
        }

        var rawTicks = _sw.ElapsedTicks;
        _sw.Restart();

        var frameMs = (double)rawTicks / Stopwatch.Frequency * 1000.0;

        _accTimeMs += frameMs;
        if (frameMs < _timeState.MinMs) _timeState.MinMs = frameMs;
        if (frameMs > _timeState.MaxMs) _timeState.MaxMs = frameMs;
        _framesInWindow++;

        if (EngineTime.FrameIndex >= _frameIndex + _framesPerReport)
        {
            OnReport();
            _frameIndex = EngineTime.FrameIndex;
        }
    }

    private void UpdateMemoryState()
    {
        ref var ms = ref _memoryState;
        var lastGc = ms.LastGcSample;
        var lastThreadAllocBytes = ms.AllocatedBytes;
        ms.LastGcSample = GetCurrentGcSample();
        ms.Delta = GcSample.GetDelta(ms.LastGcSample, lastGc, out ms.DeltaGcSample);
        ms.AllocatedBytes = GC.GetAllocatedBytesForCurrentThread();
        ms.DeltaAllocBytes = ms.AllocatedBytes - lastThreadAllocBytes;
    }

    private void UpdateTimeState()
    {
        ref var ts = ref _timeState;
        ts.AvgMs = _accTimeMs / _framesInWindow;
        ts.BudgetUsage = (ts.AvgMs / _targetFrameMs) * 100.0;
        ts.Fps = 1000.0 / ts.AvgMs;
    }

    private void OnReport()
    {
        UpdateTimeState();
        UpdateMemoryState();
        ref var ts = ref _timeState;
        ref var ms = ref _memoryState;

        var memKb = ms.DeltaAllocBytes / 1024.0;
        var gcByte = (byte)Math.Min(ms.Delta, 255);

        var isSpike = ts.MaxMs > (ts.AvgMs * 2.0);

        var sample = new FrameMetricSample(
            ts.AvgMs, ts.MinMs, ts.MaxMs, ts.BudgetUsage,
            ms.DeltaAllocBytes, gcByte, isSpike
        );

        _history.Add(sample);
        _onSample?.Invoke(sample);

        LogResult(in ts, in ms, memKb, isSpike);

        _accTimeMs = 0;

        ts.MinMs = double.MaxValue;
        ts.MaxMs = double.MinValue;
        _framesInWindow = 0;
    }

    private static void LogResult(
        in TimeState ts,
        in MemoryState ms,
        double memKb,
        bool isSpike)
    {
        Span<char> buffer = stackalloc char[256];
        var builder = ZaSpanStringBuilder.Create(buffer);

        builder.Append(isSpike ? "[SPIKE]" : "[Frame]");

        builder.Append(" SYS: ")
            .Append(ts.AvgMs, "F4")
            .Append("ms (Min:").AppendIf(ts.MinMs < 10, " ").Append(ts.MinMs, "F2")
            .Append(" Max:").AppendIf(ts.MaxMs < 10, " ").Append(ts.MaxMs, "F2")
            .Append(") | ");

        builder.Append("Load: ").Append(ts.BudgetUsage, "F1").Append("% | ");

        builder.Append("FPS: ").Append(ts.Fps, "F1").Append(" | ");

        builder.Append("Mem: ").Append(memKb, "F1").Append(" KB");

        if (ms.Delta > 0)
        {
            builder.Append(" [GC: ")
                .Append(ms.DeltaGcSample.Gen0).Append("/")
                .Append(ms.DeltaGcSample.Gen1).Append("/")
                .Append(ms.DeltaGcSample.Gen2)
                .Append("]");
        }

        Console.WriteLine(builder.AsSpan());
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

    private struct MemoryState
    {
        public long AllocatedBytes;
        public long DeltaAllocBytes;
        public GcSample LastGcSample;
        public GcSample DeltaGcSample;
        public int Delta;
    }

    private struct TimeState
    {
        public double AvgMs;
        public double MinMs;
        public double MaxMs;
        public double BudgetUsage;
        public double Fps;
    }
}