using System.Diagnostics;
using ConcreteEngine.Graphics.Gfx.Definitions;
using ConcreteEngine.Graphics.Gfx.Resources;

namespace ConcreteEngine.Graphics.Diagnostic;

public sealed record GfxDebugLog(long Time, ResourceKind Kind, string Message, string? Source, string? Detailed = null)
{
    public const string StoreSource = "Store";

    public static string FormatId(int id) => id.ToString().PadRight(2);

    public static GfxDebugLog MakeStore(ResourceKind kind, string message, string? detailed = null)
        => new (DateTimeOffset.Now.ToUnixTimeMilliseconds(), kind, message, StoreSource, detailed);

    public string ToDebugString()
    {
        var kindStr = Kind != ResourceKind.Invalid ?  Kind.ToSimpleName().PadRight(9) : string.Empty;
        
        var t = DateTimeOffset.FromUnixTimeMilliseconds(Time);
        return $"[{t:HH:mm:ss.fff}] [{Source ?? "General"}] {kindStr} {Message}";
    }
}

public static class GfxDebugMetrics
{
    private static readonly Dictionary<ResourceKind, StoreMetrics> Stores = new(8);
    public static IReadOnlyDictionary<ResourceKind, StoreMetrics> GetStoreMetrics() => Stores;

    public static Queue<GfxDebugLog> LogQueue = new(4);
    
    private static bool _logEnabled = false;

    public static bool LogEnabled
    {
        get => _logEnabled;
        set
        {
            if(_logEnabled == value) return;
            LogQueue.Clear();
            _logEnabled = value;
        }
    }

    internal static void Log( GfxDebugLog log)
    {
        if(!LogEnabled) return;
        if (LogQueue.Count > 100)
        {
            Debug.Assert(false);
            return;
        }

        LogQueue.Enqueue(log);
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
    public int GfxStoreCount { get; set; }
    public int GfxStoreFree { get; set; }

    public int BackendStoreCount { get; set; }
    public int BackendStoreFree { get; set; }
}