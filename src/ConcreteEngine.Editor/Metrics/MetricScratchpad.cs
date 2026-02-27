using System.Diagnostics;
using System.Runtime;
using ConcreteEngine.Core.Diagnostics.Metrics;
using ConcreteEngine.Core.Engine.Assets.Data;
using ConcreteEngine.Graphics;
using ConcreteEngine.Graphics.Diagnostic;

namespace ConcreteEngine.Editor.Metrics;

public static class MetricScratchpad
{
    public static PerformanceMetric Performance;
    public static GpuFrameMetaBundle GpuFrameMeta;
    public static FrameMeta FrameMeta;
    public static SceneMeta SceneMeta;
}


public interface IMetricSystem
{
    void BindStore(int gfxStoreCount, int assetStoreCount, Action<GfxStoreMeta[], AssetsMetaInfo[]> refreshStore);
}

internal sealed class MetricSystem : IMetricSystem
{
    public static readonly MetricSystem Instance = new ();

    public RuntimeSample Runtime;
    public PerformanceMetric Performance;

    private int _totalTicks = 0;
    public bool Enabled { get; set; }
    public bool IsWarmup => _totalTicks > 40;

    public StoreMetrics? Stores;
    public readonly PerformanceSession PerfSession = new();
    
    private MetricSystem(){}

    public void Tick()
    {
        _totalTicks++;
        Performance = MetricScratchpad.Performance;
        PerfSession.Update(in Performance);
    }

    public void BindStore(int gfxStoreCount, int assetStoreCount, Action<GfxStoreMeta[], AssetsMetaInfo[]> refreshStore)
    {
        Stores = new StoreMetrics(gfxStoreCount, assetStoreCount, refreshStore);
    }
}


public sealed class StoreMetrics(int gfxStoreCount, int assetStoreCount, Action<GfxStoreMeta[], AssetsMetaInfo[]> onRefresh)
{
    public readonly GfxStoreMeta[] Gfx = new GfxStoreMeta[gfxStoreCount];
    public readonly AssetsMetaInfo[] Assets = new AssetsMetaInfo[assetStoreCount];
    public readonly string[] GfxMetaDescriptions = new string[gfxStoreCount];

    public long LastFetched { get; private set; }= 0;

    internal void Refresh()
    {
        onRefresh(Gfx, Assets);
        for (var i = 0; i < GfxMetaDescriptions.Length; i++)
            GfxMetaDescriptions[i] = MetricsFormatter.FormatGfxStoreMeta(in Gfx[i]);

        LastFetched = Stopwatch.GetTimestamp();
    }
}