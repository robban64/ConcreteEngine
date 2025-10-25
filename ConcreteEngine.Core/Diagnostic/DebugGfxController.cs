using ConcreteEngine.Common.Diagnostics;
using ConcreteEngine.Graphics.Diagnostic;
using Core.DebugTools.Data;

namespace ConcreteEngine.Core.Diagnostic;

internal static class DebugGfxController
{
    internal static void DrainGfxStoreMetrics(MetricData data)
    {
        Span<GfxStoreMetricsPayload> span = stackalloc GfxStoreMetricsPayload[GfxDebugMetrics.StoreCount];
        GfxDebugMetrics.DrainStoreMetrics(span);

        if (data.GfxStoreMetrics.Length != span.Length)
        {
            data.GfxStoreMetrics = new DebugGfxStoreMetrics[span.Length];
            var names = GfxDebugMetrics.GetStoreNames();
            for (int i = 0; i < names.Length; i++)
            {
                ref readonly var n = ref names[i];
                data.GfxStoreMetrics[i] = new DebugGfxStoreMetrics(n.Item1, n.Item2, (byte)(i + 1));
            }
        }
        
        var result = data.GfxStoreMetrics;
        for (int i = 0; i < span.Length; i++)
        {
            var res = result[i];
            ref readonly var metrics = ref span[i];
            res.GfxStoreMetrics = metrics.Fk;
            res.BackendStoreMetrics = metrics.Bk;
            res.SpecialMetric = metrics.Special;
        }
    }

}