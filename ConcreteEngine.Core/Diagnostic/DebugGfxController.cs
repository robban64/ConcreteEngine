using ConcreteEngine.Graphics.Diagnostic;
using Core.DebugTools.Data;

namespace ConcreteEngine.Core.Diagnostic;

internal static class DebugGfxController
{
    internal static void DrainGfxStoreMetrics(List<DebugStoreMetrics> result)
    {
        Span<GfxStoreMetricsPayload> span = stackalloc GfxStoreMetricsPayload[GfxDebugMetrics.StoreCount];
        GfxDebugMetrics.DrainStoreMetrics(span);

        if (result.Count != span.Length)
        {
            result.Clear();
            var names = GfxDebugMetrics.GetStoreNames();
            for (int i = 0; i < names.Length; i++)
            {
                ref readonly var n = ref names[i];
                result.Add(new DebugStoreMetrics(n.Item1, n.Item2, (byte)(i + 1)));
            }
        }

        for (int i = 0; i < span.Length; i++)
        {
            ref readonly var storeMetric = ref span[i];
            var res = result[i];

            ref readonly var fk = ref storeMetric.Fk;
            ref readonly var bk = ref storeMetric.Bk;

            var gfxSpec = fk.Special;
            var specialMetric = new DebugGfxStoreMetricsRecord.SpecialMetric
                (gfxSpec.Value, gfxSpec.ResourceId, gfxSpec.Param2, (byte)gfxSpec.Kind);

            res.GfxStoreMetrics = new DebugGfxStoreMetricsRecord
                (fk.Count, fk.Alive, fk.Free, fk.Capacity, in specialMetric);
            res.BackendStoreMetrics = new DebugGfxStoreMetricsRecord
                (bk.Count, bk.Alive, bk.Free, bk.Capacity, default);
        }
    }

}