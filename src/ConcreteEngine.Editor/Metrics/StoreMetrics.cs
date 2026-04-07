using System.Diagnostics;
using ConcreteEngine.Core.Common;
using ConcreteEngine.Core.Engine.Assets.Data;
using ConcreteEngine.Graphics.Diagnostic;

namespace ConcreteEngine.Editor.Metrics;

internal sealed class StoreMetrics(
    int gfxStoreCount,
    int assetStoreCount,
    Action<GfxStoreMeta[], AssetsMetaInfo[]> onRefresh)
{
    public readonly GfxStoreMeta[] Gfx = new GfxStoreMeta[gfxStoreCount];
    public readonly AssetsMetaInfo[] Assets = new AssetsMetaInfo[assetStoreCount];
    public readonly string[] GfxMetaDescriptions = new string[gfxStoreCount];

    public long LastFetched { get; private set; } = 0;

    internal void Refresh()
    {
        if (LastFetched > 0 && TimeUtils.GetElapsedMillisecondsSince(LastFetched) < 1000)
            throw new InvalidOperationException("Refreshed too fast, should not be called frequent");

        onRefresh(Gfx, Assets);

        for (var i = 0; i < GfxMetaDescriptions.Length; i++)
            GfxMetaDescriptions[i] = MetricsFormatter.FormatGfxStoreMeta(in Gfx[i]);

        LastFetched = Stopwatch.GetTimestamp();
    }
}