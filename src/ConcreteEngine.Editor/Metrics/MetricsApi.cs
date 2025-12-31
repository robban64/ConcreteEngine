using System.Diagnostics;
using System.Runtime.CompilerServices;
using ConcreteEngine.Core.Diagnostics.Metrics;
using ConcreteEngine.Core.Diagnostics.Time;

namespace ConcreteEngine.Editor.Metrics;

public static partial class MetricsApi
{
    private static readonly List<MetricProvider> All = new(8);
    private static PerformanceSession? _performanceSession;

    private static long _currentTick = -1;

    private static FrameStepper _stepper = new(40);
    
    internal static bool HasWarmup;
    
    public static bool Enabled { get; private set; }
    public static bool HasInitialized { get; private set; }

    internal static void EnterMetricMode()
    {
        Enabled = true;
        Store.Toggle(true);
        foreach (var provider in All) provider.Toggle(true);
    }

    internal static void LeaveMetricMode()
    {
        Enabled = false;
        Store.Toggle(false);
        foreach (var provider in All) provider.Toggle(true);
    }

    internal static PerformanceSession GetPerformanceSession()
    {
        return _performanceSession ?? throw new InvalidOperationException("MetricApi has not been initialized");
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static void Tick()
    {
        if (!HasInitialized || !Enabled) return;

        if (!HasWarmup && _stepper.Tick())
        {
            HasWarmup = true;
            foreach (var it in All) it.ClearData();
            _performanceSession!.ClearCurrent();
        }
        
        var ticks = _currentTick = Stopwatch.GetTimestamp();

        Provider<PerformanceMetric>.Record!.Tick(ticks);
        _performanceSession!.Update(in Provider<PerformanceMetric>.Data);

        Provider<FrameMeta>.Record!.Tick(ticks);
        Provider<SceneMeta>.Record!.Tick(ticks);
        Provider<GpuFrameMetaBundle>.Record!.Tick(ticks);
    }


    public static void FinishSetup()
    {
        if (HasInitialized)
            throw new InvalidOperationException("MetricsApi already initialized");

        if (All.Count == 0)
            throw new InvalidOperationException("MetricApi no Provider registered");

        if (Store.Gfx is null || Store.Assets is null)
            throw new InvalidOperationException("MetricApi.Store not registered");

        if (Provider<PerformanceMetric>.Record is null)
            throw new InvalidOperationException("MetricApi PerformanceMetric not registered");

        _performanceSession = new PerformanceSession();
        _performanceSession.LoadBaseline();

        HasInitialized = true;
    }
}