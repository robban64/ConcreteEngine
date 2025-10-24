#region

using System.Diagnostics;
using ConcreteEngine.Graphics.Gfx.Definitions;
using ConcreteEngine.Graphics.Gfx.Resources;

#endregion

namespace ConcreteEngine.Graphics.Diagnostic;

public static class GfxDebugMetrics
{
    private static readonly ResourceKind[] Kinds = Enum.GetValues<ResourceKind>();

    private static readonly IStoreMetrics[] StoreMetrics = new IStoreMetrics[StoreCount];

    private static readonly List<DebugFilter> IgnoreFilter = new(4);

    public static Queue<GfxDebugLog> LogQueue { get; } = new(16);

    //public static bool MetricsEnabled { get; private set; } = true;
    private static bool _logEnabled = false;

    public static bool LogEnabled
    {
        get => _logEnabled;
        set
        {
            if (_logEnabled == value) return;
            LogQueue.Clear();
            _logEnabled = value;
        }
    }

    public static int StoreCount => Kinds.Length - 1;
    public static ReadOnlySpan<ResourceKind> GetResourceKinds() => Kinds;
    internal static IStoreMetrics GetStoreMetrics(ResourceKind kind) => StoreMetrics[(int)kind - 1];

    internal static IStoreMetrics GetStoreMetrics<TId>() where TId : unmanaged, IResourceId =>
        StoreMetrics[(int)TId.Kind - 1];

    public static ReadOnlySpan<(string, string)> GetStoreNames()
    {
        var names = new (string, string)[StoreCount];
        for (int i = 0; i < StoreMetrics.Length; i++)
        {
            var it = StoreMetrics[i];
            names[i] = (it.Name, it.ShortName);
        }

        return names;
    }

    public static void DrainStoreMetrics(Span<GfxStoreMetricsPayload> span)
    {
        for (int i = 0; i < StoreMetrics.Length; i++)
            StoreMetrics[i].GetResult(out span[i]);
    }

    
    
    internal static void BindStore<TId, TMeta, THandle>(
        GetGfxStoreDel<TId, TMeta> gfx,
        GetBackendStoreDel<TId, THandle> bk,
        GetSpecialMetric<TMeta> specialMetricDel)
        where TId : unmanaged, IResourceId
        where TMeta : unmanaged, IResourceMeta
        where THandle : unmanaged, IResourceHandle, IEquatable<THandle>
    {
        var kind = (int)TId.Kind;
        ArgumentOutOfRangeException.ThrowIfLessThan(kind, 1, nameof(kind));
        StoreMetrics[kind - 1] = new StoreMetrics<TId, TMeta, THandle>(TId.Kind, gfx, bk, specialMetricDel);
    }

    internal static void Log(GfxDebugLog log)
    {
        if (!LogEnabled) return;
        if (LogQueue.Count > 100)
        {
            Debug.Assert(false);
            return;
        }

        if (!FilterLog(in log))
            return;

        LogQueue.Enqueue(log);
    }

    private static bool FilterLog(in GfxDebugLog log)
    {
        foreach (var it in IgnoreFilter)
        {
            var validKind = it.Kind == 0 || it.Kind == (byte)log.Kind;
            var validLayer = it.Layer == 0 || it.Layer == (byte)log.Layer;
            var validSource = it.Source == 0 || it.Source == (byte)log.Source;
            var validAction = it.Action == 0 || it.Action == (byte)log.Action;
            if (validKind && validLayer && validSource && validAction) return true;
        }

        return false;
    }

    public static void ToggleLog(
        bool enabled,
        ResourceKind kind = 0,
        GfxLogLayer layer = 0,
        GfxLogSource source = 0,
        GfxLogAction action = 0)
    {
        var rule = new DebugFilter(kind, layer, source, action);

        if (enabled)
        {
            for (var i = 0; i < IgnoreFilter.Count; i++)
                if (IgnoreFilter[i].Equals(rule))
                {
                    IgnoreFilter.RemoveAt(i);
                    return;
                }
        }
        else
        {
            foreach (var t in IgnoreFilter)
            {
                if (t == rule) return;
            }

            IgnoreFilter.Add(rule);
        }
    }

    public static void ToggleLog(GfxLogLayer layer, bool enabled) => ToggleLog(enabled, layer: layer);
    public static void ToggleLog(GfxLogAction action, bool enabled) => ToggleLog(enabled, action: action);
    public static void ToggleLog(GfxLogSource source, bool enabled) => ToggleLog(enabled, source: source);
    public static void ToggleLog(ResourceKind kind, bool enabled) => ToggleLog(enabled, kind: kind);
}