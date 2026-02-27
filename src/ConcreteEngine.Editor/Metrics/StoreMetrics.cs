using System.Diagnostics;
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
        onRefresh(Gfx, Assets);
        for (var i = 0; i < GfxMetaDescriptions.Length; i++)
            GfxMetaDescriptions[i] = MetricsFormatter.FormatGfxStoreMeta(in Gfx[i]);

        LastFetched = Stopwatch.GetTimestamp();
    }
}