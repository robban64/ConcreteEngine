#region

using System.Diagnostics;
using ConcreteEngine.Graphics.Gfx.Definitions;
using ConcreteEngine.Graphics.Gfx.Resources;

#endregion

namespace ConcreteEngine.Graphics.Diagnostic;

internal readonly record struct DebugFilter(byte Kind, byte Layer, byte Source, byte Action)
{
    public DebugFilter(ResourceKind Kind, GfxLogLayer Layer, GfxLogSource Source, GfxLogAction Action)
        : this((byte)Kind, (byte)Layer, (byte)Source, (byte)Action)
    {
    }
}

public static class GfxDebugMetrics
{
    private sealed record StoreBindingRecord(Delegate GetGfxStore, Delegate GetBackendStore);

    private static readonly ResourceKind[] Kinds = Enum.GetValues<ResourceKind>();

    private static readonly StoreMetrics[] Stores = new StoreMetrics[Kinds.Length - 1];

    private static readonly Dictionary<ResourceKind, StoreBindingRecord> StoreBindings = new(Kinds.Length - 1);

    public static ReadOnlySpan<StoreMetrics> GetStoreMetrics() => Stores;

    private static readonly List<DebugFilter> IgnoreFilter = new(4);

    public static Queue<GfxDebugLog> LogQueue { get; } = new(16);

    //public static bool MetricsEnabled { get; private set; } = true;
    private static bool _logEnabled = false;
    
    public static ReadOnlySpan<ResourceKind> GetResourceKinds() => Kinds;
    public static StoreMetrics GetStoreMetrics(ResourceKind kind) => Stores[(int)kind - 1];
    public static StoreMetrics GetStoreMetrics<TId>() where TId : unmanaged, IResourceId => Stores[(int)TId.Kind - 1];

    public static TMeta GetStoreMeta<TId, TMeta>(TId id) where TId : unmanaged, IResourceId
        where TMeta : unmanaged, IResourceMeta
    {
        var del = (GetGfxStoreDel<TId, TMeta>)StoreBindings[TId.Kind].GetGfxStore;
        return del().GetMeta(id);
    }

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
    
    
    internal static void RegisterStore<TId, TMeta,THandle>
        (GetGfxStoreDel<TId, TMeta> gfx, GetBackendStoreDel<TId, THandle> bk)
        where TId : unmanaged, IResourceId
        where TMeta : unmanaged, IResourceMeta
        where THandle : unmanaged, IResourceHandle, IEquatable<THandle>
    {
        var kind = (int)TId.Kind;
        ArgumentOutOfRangeException.ThrowIfLessThan(kind, 1, nameof(kind));

        StoreBindings.Add(TId.Kind,new StoreBindingRecord(gfx,bk));
        Stores[kind - 1] = new StoreMetrics(TId.Kind);
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
            // remove exact rule if present
            for (var i = 0; i < IgnoreFilter.Count; i++)
                if (IgnoreFilter[i].Equals(rule)) { IgnoreFilter.RemoveAt(i); return; }
        }
        else
        {
            // add if not already present
            for (var i = 0; i < IgnoreFilter.Count; i++)
                if (IgnoreFilter[i].Equals(rule)) return;
            IgnoreFilter.Add(rule);
        }
    }

    // --- Back-compat thin wrappers (optional; delete once call sites are updated) ---
    public static void ToggleLog(GfxLogLayer layer, bool enabled) =>
        ToggleLog(enabled, layer: layer);

    public static void ToggleLog(GfxLogAction action, bool enabled) =>
        ToggleLog(enabled, action: action);

    public static void ToggleLog(GfxLogSource source, bool enabled) =>
        ToggleLog(enabled, source: source);

    public static void ToggleLog(ResourceKind kind, bool enabled) =>
        ToggleLog(enabled, kind: kind);
}

public sealed class StoreMetrics(ResourceKind kind)
{
    public ResourceKind Kind { get; } = kind;
    public string Name { get; } = kind.ToLogName();
    public string ShortName { get; } = kind.ToLogName(true);

    public int GfxCount { get; set; }
    public int GfxFree { get; set; }

    public int BkCount { get; set; }
    public int BkFree { get; set; }
}