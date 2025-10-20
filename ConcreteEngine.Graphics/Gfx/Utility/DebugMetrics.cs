using System.Diagnostics;
using ConcreteEngine.Graphics.Gfx.Definitions;
using ConcreteEngine.Graphics.Gfx.Resources;

namespace ConcreteEngine.Graphics.Gfx.Utility;

public static class GfxDebugMetrics
{
    private static readonly Dictionary<ResourceKind, StoreMetrics> Stores = new(8);
    public static IReadOnlyDictionary<ResourceKind, StoreMetrics> GetStoreMetrics() => Stores;

    internal static void RegisterStore<TId>() where TId : unmanaged, IResourceId
    {
        if(Stores.ContainsKey(TId.Kind)) return;
        Stores[TId.Kind] = new StoreMetrics();
    }
    public static StoreMetrics GetStoreMetrics(ResourceKind kind) => Stores[kind];
    public static StoreMetrics GetStoreMetrics<TId>() where TId : unmanaged, IResourceId => Stores[TId.Kind];
}

public record struct BackendStoreEvent(long Time, uint Handle, ushort Gen, bool Alive)
{
    public static BackendStoreEvent Create( uint handle, ushort gen,  bool alive)
        => new(Stopwatch.GetTimestamp(), handle, gen, alive);
}

public record struct FrontendStoreEvent(long Time, int Id, int Slot, ushort Gen, ResourceKind Kind)
{
    public static FrontendStoreEvent Create(int id, int slot, ushort gen, ResourceKind kind)
        => new(Stopwatch.GetTimestamp(),id, slot, gen, kind);
}

public sealed class StoreMetrics
{
    public int GfxStoreCount { get; set; }
    public int BackendStoreCount { get; set; }
    
    public int GfxStoreFree { get; set; }
    public int BackendStoreFree { get; set; }

    public FrontendStoreEvent LastAddedGfx { get; set; }
    public FrontendStoreEvent LastReplacedGfx { get; set; }
    public FrontendStoreEvent LastRemovedGfx { get; set; }

    public BackendStoreEvent LastAddedBackend { get; set; }
    public BackendStoreEvent LastReplacedBackend { get; set; }
    public BackendStoreEvent LastRemovedBackend { get; set; }
}