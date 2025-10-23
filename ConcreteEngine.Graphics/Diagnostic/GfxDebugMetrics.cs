#region

using System.Diagnostics;
using ConcreteEngine.Graphics.Gfx.Definitions;
using ConcreteEngine.Graphics.Gfx.Resources;

#endregion

namespace ConcreteEngine.Graphics.Diagnostic;

public static class GfxDebugMetrics
{
    private static readonly Dictionary<ResourceKind, StoreMetrics> Stores = new(8);
    public static IReadOnlyDictionary<ResourceKind, StoreMetrics> GetStoreMetrics() => Stores;

    private static HashSet<(GfxLogSource, GfxLogLayer)> IgnoreSourceLayer { get; } = [];
    private static HashSet<GfxLogAction> IgnoreAction { get; } = [];
    private static HashSet<ResourceKind> IgnoreKind { get; } = [];

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

    internal static void Log(GfxDebugLog log)
    {
        if (!LogEnabled) return;
        if (LogQueue.Count > 100)
        {
            Debug.Assert(false);
            return;
        }

        if (IgnoreSourceLayer.Contains((log.Source, log.Layer))
            || IgnoreAction.Contains(log.Action)
            || IgnoreKind.Contains(log.Kind))
            return;

        LogQueue.Enqueue(log);
    }

    public static void ToggleLog(GfxLogSource source, GfxLogLayer layer, bool enabled)
    {
        var key = (source, layer);
        if (enabled) IgnoreSourceLayer.Remove(key);
        else IgnoreSourceLayer.Add(key);
    }

    public static void ToggleLog(GfxLogAction action, bool enabled)
    {
        if (enabled) IgnoreAction.Remove(action);
        else IgnoreAction.Add(action);
    }

    public static void ToggleLog(ResourceKind kind, bool enabled)
    {
        if (enabled) IgnoreKind.Remove(kind);
        else IgnoreKind.Add(kind);
    }

    internal static void RegisterStore<TId>() where TId : unmanaged, IResourceId
    {
        if (Stores.ContainsKey(TId.Kind)) return;
        Stores[TId.Kind] = new StoreMetrics();
    }

    public static StoreMetrics GetStoreMetrics(ResourceKind kind) => Stores[kind];
    public static StoreMetrics GetStoreMetrics<TId>() where TId : unmanaged, IResourceId => Stores[TId.Kind];
}

public sealed class StoreMetrics
{
    public int GfxCount { get; set; }
    public int GfxFree { get; set; }

    public int BkCount { get; set; }
    public int BkFree { get; set; }
}