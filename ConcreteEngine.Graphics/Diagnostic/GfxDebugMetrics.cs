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
    private static readonly ResourceKind[] Kinds = Enum.GetValues<ResourceKind>();

    private static readonly IStoreMetrics[] Stores = new IStoreMetrics[Kinds.Length - 1];

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

    public static ReadOnlySpan<ResourceKind> GetResourceKinds() => Kinds;
    public static IStoreMetrics GetStoreMetrics(ResourceKind kind) => Stores[(int)kind - 1];
    public static IStoreMetrics GetStoreMetrics<TId>() where TId : unmanaged, IResourceId => Stores[(int)TId.Kind - 1];
/*
    public static TMeta GetStoreMeta<TId, TMeta>(TId id) where TId : unmanaged, IResourceId
        where TMeta : unmanaged, IResourceMeta
    {
        var del = (GetGfxStoreDel<TId, TMeta>)Stores[(int)TId.Kind - 1];
        return del().GetMeta(id);
    }
*/
    public static ReadOnlySpan<IStoreMetrics> GetStoreMetrics()
    {
        foreach (var it in Stores)
            it.Invoke();
        return Stores;
    }


    internal static void RegisterStore<TId, TMeta, THandle>(GetGfxStoreDel<TId, TMeta> gfx,
        GetBackendStoreDel<TId, THandle> bk)
        where TId : unmanaged, IResourceId
        where TMeta : unmanaged, IResourceMeta
        where THandle : unmanaged, IResourceHandle, IEquatable<THandle>
    {
        var kind = (int)TId.Kind;
        ArgumentOutOfRangeException.ThrowIfLessThan(kind, 1, nameof(kind));

        Stores[kind - 1] = new StoreMetrics<TId, TMeta, THandle>(TId.Kind, gfx, bk);
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
                if (t == rule)
                    return;

            IgnoreFilter.Add(rule);
        }
    }

    public static void ToggleLog(GfxLogLayer layer, bool enabled) => ToggleLog(enabled, layer: layer);
    public static void ToggleLog(GfxLogAction action, bool enabled) => ToggleLog(enabled, action: action);
    public static void ToggleLog(GfxLogSource source, bool enabled) => ToggleLog(enabled, source: source);
    public static void ToggleLog(ResourceKind kind, bool enabled) => ToggleLog(enabled, kind: kind);
}

public readonly record struct GfxStoreMetricsRecord(int Count, int Alive, int Free, int Capacity);

public interface IStoreMetrics
{
    ResourceKind Kind { get; }
    string Name { get; }
    string ShortName { get; }

    GfxStoreMetricsRecord GfxStoreMetrics { get; }
    GfxStoreMetricsRecord BackendStoreMetrics { get; }
    
    void Invoke();
}

internal sealed class StoreMetrics<TId, TMeta, THandle>(
    ResourceKind kind,
    GetGfxStoreDel<TId, TMeta> getGfxStore,
    GetBackendStoreDel<TId, THandle> getBackendStore) : IStoreMetrics
    where TId : unmanaged, IResourceId
    where TMeta : unmanaged, IResourceMeta
    where THandle : unmanaged, IResourceHandle, IEquatable<THandle>

{
    internal GetGfxStoreDel<TId, TMeta> GetBackendStore { get; } = getGfxStore;
    internal GetBackendStoreDel<TId, THandle> GetGfxStore { get; } = getBackendStore;
    public ResourceKind Kind { get; } = kind;
    public string Name { get; } = kind.ToLogName();
    public string ShortName { get; } = kind.ToLogName(true);
    public GfxStoreMetricsRecord GfxStoreMetrics { get; private set; }
    public GfxStoreMetricsRecord BackendStoreMetrics { get; private set; }
    
    public void Invoke()
    {
        var gfx = GetGfxStore();
        var bk = GetBackendStore();
        GfxStoreMetrics = new GfxStoreMetricsRecord(gfx.Count,gfx.GetAliveCount(),gfx.FreeCount,gfx.Capacity);
        BackendStoreMetrics = new GfxStoreMetricsRecord(bk.Count,bk.GetAliveCount(),bk.FreeCount,bk.Capacity);

    }
}