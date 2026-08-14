using System.Runtime.CompilerServices;
using ConcreteEngine.Core.Common;
using ConcreteEngine.Core.Common.Collections;
using ConcreteEngine.Core.Common.Memory;
using ConcreteEngine.Core.Diagnostics.Logging;
using ConcreteEngine.Core.Diagnostics.Metrics;
using ConcreteEngine.Graphics.Configuration;
using ConcreteEngine.Graphics.Diagnostic;

namespace ConcreteEngine.Graphics.Gfx;

internal interface IGfxResourceStore : IDisposable
{
    GraphicsKind GraphicsKind { get; }
    int Count { get; }
    int FreeCount { get; }
    int Capacity { get; }

    int GetAliveCount();

    void BindOnUpdateCallback(Action<int> callback);
    void FillGfxStoreMeta(out GfxStoreMeta data);
}

internal sealed unsafe class GfxStore<TMeta> : IGfxResourceStore where TMeta : unmanaged, IResourceMeta
{
    public static GfxStore<TMeta> Instance = null!;

    private struct Entry
    {
        public NativeHandle<TMeta> Handle;
        public TMeta Meta;
    }

    public int Count { get; private set; }

    private Entry* _entries;
    private NativeArray<byte> _memory;

    private readonly Stack<int> _free;

    private Action<int>? _onUpdate;

    internal GfxStore(int initialCapacity)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(initialCapacity, 4);
        ArgumentOutOfRangeException.ThrowIfGreaterThan(initialCapacity, GfxLimits.StoreLimit);
        if (TMeta.ResourceKind == GraphicsKind.Invalid) Throwers.InvalidOperation(nameof(GraphicsKind));

        if (Instance != null!) Throwers.InvalidOperation(nameof(Instance));
        Instance = this;

        _memory = NativeArray.Allocate(initialCapacity * Unsafe.SizeOf<Entry>());
        _entries = (Entry*)_memory.Ptr;

        _free = new Stack<int>();
    }

    public GraphicsKind GraphicsKind => TMeta.ResourceKind;

    public int ActiveCount => Count - _free.Count;
    public int FreeCount => _free.Count;
    public int Capacity => _memory.Length > 0 ? _memory.Length / Unsafe.SizeOf<Entry>() : 0;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public NativeHandle<TMeta> GetHandle(GfxId<TMeta> id) => _entries[id.Index()].Handle;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public ref readonly TMeta GetMeta(GfxId<TMeta> id) => ref _entries[id.Index()].Meta;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public NativeHandle<TMeta> GetHandleAndMeta(GfxId<TMeta> id, out TMeta meta)
    {
        ref readonly var it = ref _entries[id.Index()];
        meta = it.Meta;
        return it.Handle;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public NativeHandle<TMeta> TryGet(GfxId<TMeta> id, out TMeta result)
    {
        if (id < (uint)Count) return GetHandleAndMeta(id, out result);
        Unsafe.SkipInit(out result);
        return default;
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    public GfxId<TMeta> Add(in TMeta meta, NativeHandle<TMeta> handle)
    {
        ArgumentOutOfRangeException.ThrowIfEqual(handle.IsValid(), false, nameof(handle));
        ArgumentOutOfRangeException.ThrowIfZero(handle.Value, nameof(handle));

        var index = AllocateNext();
        _entries[index].Handle = handle;
        _entries[index].Meta = meta;

        var id = new GfxId<TMeta>((ushort)(index + 1));
        GfxLog.LogGfxStore(id, handle, GraphicsKind.ToLogTopic(), LogAction.Add);
        return id;
    }


    [MethodImpl(MethodImplOptions.NoInlining)]
    public NativeHandle Remove(GfxId<TMeta> id, out TMeta oldMeta)
    {
        ArgumentOutOfRangeException.ThrowIfEqual(id, 0);

        var handle = _entries[id.Index()].Handle;
        oldMeta = _entries[id.Index()].Meta;

        _entries[id.Index()] = default;

        Count = SlotHelper.FreeSlot(_free, id.Index(), Count);

        GfxLog.LogGfxStore(id, handle, GraphicsKind.ToLogTopic(), LogAction.Remove);
        return handle;
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    public GfxId<TMeta> Replace(GfxId<TMeta> id, in TMeta newMeta, NativeHandle<TMeta> newHandle)
    {
        ArgumentOutOfRangeException.ThrowIfEqual(id.Id, 0, nameof(id));
        ArgumentOutOfRangeException.ThrowIfZero(newHandle.Value, nameof(newHandle));

        _entries[id.Index()].Handle = newHandle;
        _entries[id.Index()].Meta = newMeta;

        GfxLog.LogGfxStore(id, newHandle, GraphicsKind.ToLogTopic(), LogAction.Replace);
        _onUpdate?.Invoke(id);
        return id;
    }

    public void ReplaceMeta(GfxId<TMeta> id, in TMeta newMeta, out TMeta oldMeta)
    {
        ArgumentOutOfRangeException.ThrowIfEqual(id, 0);

        oldMeta = _entries[id.Index()].Meta;
        _entries[id.Index()].Meta = newMeta;
        _onUpdate?.Invoke(id);
    }

    public int GetAliveCount()
    {
        var count = 0;
        var length = Count;
        for (var i = 0; i < length; i++)
        {
            if (_entries[i].Handle.IsValid()) count++;
        }

        return count;
    }

    public void BindOnUpdateCallback(Action<int> callback)
    {
        ArgumentNullException.ThrowIfNull(callback);
        if (_onUpdate is not null) Throwers.InvalidOperation(nameof(_onUpdate));
        _onUpdate = callback;
    }

    private int AllocateNext()
    {
        var index = SlotHelper.NextSlot(_free, Count);
        if (index >= 0) return index;

        if (Count >= Capacity) EnsureCapacity(1);
        return Count++;
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private void EnsureCapacity(int count)
    {
        var newCount = Count + count;
        var sizeInBytes = newCount * Unsafe.SizeOf<Entry>();
        if (sizeInBytes <= _memory.Length) return;

        var newCap = CapacityUtils.CapacityGrowthToFit(_memory.Length, sizeInBytes);
        if (newCap > GfxLimits.StoreLimit * Unsafe.SizeOf<Entry>())
            Throwers.BufferOverflow(typeof(GfxStore<TMeta>).Name, newCap, GfxLimits.StoreLimit);

        GfxLog.Event(new LogEvent(0, 0, newCap, 0, 0, 0, LogTopic.ArrayBuffer, LogScope.Gfx, LogAction.Resize,
            LogLevel.Warn));

        _memory.ReAlloc(newCap, true);
        _entries = (Entry*)_memory.Ptr;
    }


    public void Dispose()
    {
        _memory.Dispose();
        _entries = null;
    }

    public void FillGfxStoreMeta(out GfxStoreMeta data)
    {
        data.Fk = new CollectionSample(Count, Capacity, GetAliveCount(), FreeCount);
        data.Kind = GraphicsKind;
        data.MetaInfo = GfxMetrics.GetSpecialMetric(GraphicsKind);
    }
}