using System.Diagnostics;
using ConcreteEngine.Core.Common;
using ConcreteEngine.Core.Diagnostics.Metrics;
using ConcreteEngine.Editor.Utils;
using ConcreteEngine.Engine.Metadata;

namespace ConcreteEngine.Editor.Metrics;

public static class MetricsApi
{
    private static readonly List<MetricProvider> All = new(8);
    private static PerformanceSession? _performanceSession;
    private static string[] _gfxMetaDescriptions = [];

    private static long _currentTick = -1;

    public static bool Enabled { get; private set; }
    public static bool HasInitialized { get; private set; }
    
    internal static void EnterMetricMode()
    {
        Enabled = true;
        Store.Gfx?.Toggle(true);
        Store.Assets?.Toggle(true);
        foreach (var provider in All) provider.Toggle(true);

        for (int i = 0; i < _gfxMetaDescriptions.Length; i++)
            _gfxMetaDescriptions[i] = MetricsFormatter.FormatGfxStoreMeta(in Store.Gfx!.Data[i]);

    }
    
    internal static void LeaveMetricMode()
    {
        Enabled = false;
        Store.Gfx?.Toggle(false);
        Store.Assets?.Toggle(false);
        foreach (var provider in All) provider.Toggle(true);
        
        _gfxMetaDescriptions.AsSpan().Clear();

    }

    internal static PerformanceSession GetPerformanceSession()
    {
        return _performanceSession ?? throw new InvalidOperationException("MetricApi has not been initialized");
    }
    
    
    public static void FinishSetup()
    {
        if (HasInitialized)
            throw new InvalidOperationException("MetricsApi already initialized");

        if (All.Count == 0)
            throw new InvalidOperationException("MetricApi no Provider registered");

        if (Store.Gfx is null || Store.Assets is null)
            throw new InvalidOperationException("MetricApi.Store not registered");

        foreach (var provider in All)
        {
            if (provider is PollMetricProvider<PerformanceMetric> performanceProvider)
            {
                _performanceSession = new PerformanceSession(performanceProvider);
                break;
            }
        }

        if (_performanceSession is null)
            throw new InvalidOperationException("MetricApi PerformanceMetric not registered");

        _performanceSession.LoadBaseline();

        HasInitialized = true;
    }

    public static class Store
    {
        internal static EventMetricProvider<GfxStoreMeta>? Gfx;
        internal static EventMetricProvider<AssetStoreMeta>? Assets;

        internal static ReadOnlySpan<string> GfxMetaDescriptions => _gfxMetaDescriptions;

        public static void RegisterGfx(int count, Action<Span<GfxStoreMeta>> onRequestRefresh)
        {
            if(Gfx is not null) throw new InvalidOperationException("MetricApi GfxStore already initialized");
            Gfx = new EventMetricProvider<GfxStoreMeta>(count, onRequestRefresh);
            _gfxMetaDescriptions = new string[count];
        }
        
        public static void RegisterAsset(int count, Action<Span<AssetStoreMeta>> onRequestRefresh)
        {
            if(Assets is not null) throw new InvalidOperationException("MetricApi GfxStore already initialized");
            Assets = new EventMetricProvider<AssetStoreMeta>(count, onRequestRefresh);
        }


        private static void OnDataChange()
        {
        }
    }


    public static class Provider<T> where T : unmanaged
    {
        internal static PollMetricProvider<T>? Record;

        public static void Register(int intervalTicks, FuncFill<T> fetch)
        {
            if (HasInitialized) throw new InvalidOperationException("MetricApi is initialized");
            if (Record != null) All.Remove(Record);
            Record = new PollMetricProvider<T>(intervalTicks, fetch);
            All.Add(Record);
        }
    }


    internal static void Tick()
    {
        if (!HasInitialized || !Enabled) return;
        _currentTick = Stopwatch.GetTimestamp();

        _performanceSession!.Update();
        foreach (var provider in All)
            provider.Tick(_currentTick);
    }

}