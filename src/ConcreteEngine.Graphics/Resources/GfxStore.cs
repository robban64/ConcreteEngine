using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using ConcreteEngine.Core.Common;
using ConcreteEngine.Core.Common.Collections;
using ConcreteEngine.Core.Common.Memory;
using ConcreteEngine.Core.Diagnostics.Logging;
using ConcreteEngine.Core.Diagnostics.Metrics;
using ConcreteEngine.Graphics.Configuration;
using ConcreteEngine.Graphics.Diagnostic;
using ConcreteEngine.Graphics.Gfx;
using ConcreteEngine.Graphics.Handles;

namespace ConcreteEngine.Graphics.Resources;

internal interface IGfxResourceStore : IDisposable
{
    GraphicsKind GraphicsKind { get; }
    int Count { get; }
    int FreeCount { get; }
    int Capacity { get; }

    int GetAliveCount();

    void BindOnUpdateCallback(Action<int> callback);

    NativeHandle Remove(GfxId id);
     void FillGfxStoreMeta( out GfxStoreMeta data);
}

internal sealed class GfxStore<TMeta> : IGfxResourceStore where TMeta : unmanaged, IResourceMeta
{
    public static GfxStore<TMeta> Instance = null!;
    
    private struct Entry
    {
        public NativeHandle Handle;
        public TMeta Meta;
    }
    
    private NativeArray<Entry> _data;
    private readonly Stack<int> _free;

    private Action<int>? _onUpdate;

    public int Count { get; private set; }
    public GraphicsKind GraphicsKind { get; } = TMeta.ResourceKind;

    internal GfxStore(int initialCapacity)
    {
        if(Instance != null!) Throwers.InvalidOperation(nameof(Instance)); 
        ArgumentOutOfRangeException.ThrowIfLessThan(initialCapacity, 4);
        ArgumentOutOfRangeException.ThrowIfGreaterThan(initialCapacity, GfxLimits.StoreLimit);

        if (GraphicsKind == GraphicsKind.Invalid) Throwers.InvalidOperation(nameof(GraphicsKind));

        _data = NativeArray.Allocate<Entry>(initialCapacity);
        _free = new Stack<int>();
        Instance = this;
    }

    public int ActiveCount => Count - _free.Count;
    public int FreeCount => _free.Count;
    public int Capacity => _data.Length;

   // public ReadOnlySpan<TMeta> GetMetaSpan() => MemoryMarshal.CreateReadOnlySpan(ref _data[0], Count);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public NativeHandle GetHandle(GfxId<TMeta> id) => _data[id.Index()].Handle;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public ref readonly TMeta GetMeta(GfxId<TMeta> id) => ref _data[id.Index()].Meta;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public NativeHandle GetHandleAndMeta(GfxId<TMeta> id, out TMeta meta)
    {
        var index = id.Index();
        meta = _data[index].Meta;
        return _data[index].Handle;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public NativeHandle TryGet(GfxId<TMeta> id, out TMeta result)
    {
        if (id < (uint)Count) return GetHandleAndMeta(id, out result);
        Unsafe.SkipInit(out result);
        return default;
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    public GfxId<TMeta> Add(in TMeta meta, NativeHandle handle)
    {
        ArgumentOutOfRangeException.ThrowIfEqual(handle.IsValid(), false, nameof(handle));
        ArgumentOutOfRangeException.ThrowIfZero(handle.Value, nameof(handle));

        var index = AllocateNext();
        _data[index].Handle = handle;
        _data[index].Meta = meta;

        var id = new GfxId<TMeta>((ushort)(index + 1));
        GfxLog.LogGfxStore(id, handle, GraphicsKind.ToLogTopic(), LogAction.Add);
        return id;
    }

    public NativeHandle Remove(GfxId id)
    {
        if (!id.IsValid() || id.Kind != GraphicsKind) Throwers.InvalidOperation($"Invalid handle {id}");
        return Remove(new GfxId<TMeta>(id), out _);
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    public NativeHandle Remove(GfxId<TMeta> id, out TMeta oldMeta)
    {
        ArgumentOutOfRangeException.ThrowIfEqual(id, 0);
        var index = id - 1;
        
        var handle = _data[index].Handle;
        oldMeta = _data[index].Meta;
        
        _data[index] = default;

        Count = SlotHelper.FreeSlot(_free, index, Count);

        GfxLog.LogGfxStore(id, handle, GraphicsKind.ToLogTopic(), LogAction.Remove);
        return handle;
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    public GfxId<TMeta> Replace(GfxId<TMeta> id, in TMeta newMeta, NativeHandle newHandle)
    {
        ArgumentOutOfRangeException.ThrowIfEqual(id.Id, 0, nameof(id));
        ArgumentOutOfRangeException.ThrowIfZero(newHandle.Value, nameof(newHandle));

        var index = id - 1;
        _data[index].Handle = newHandle;
        _data[index].Meta = newMeta;

        GfxLog.LogGfxStore(id, newHandle, GraphicsKind.ToLogTopic(), LogAction.Replace);
        _onUpdate?.Invoke(id);
        return id;
    }

    public void ReplaceMeta(GfxId<TMeta> id, in TMeta newMeta, out TMeta oldMeta)
    {
        ArgumentOutOfRangeException.ThrowIfEqual(id, 0);
        int index = id - 1;

        oldMeta = _data[index].Meta;
        _data[index].Meta = newMeta;
        _onUpdate?.Invoke(id);
    }

    public int GetAliveCount()
    {
        var count = 0;
        var length = Count;
        for (var i = 0; i < length; i++)
        {
            if (_data[i].Handle.IsValid()) count++;
        }

        return count;
    }

    public void BindOnUpdateCallback(Action<int> callback)
    {
        ArgumentNullException.ThrowIfNull(callback);
        if (_onUpdate is not null) Throwers.InvalidOperation(nameof(_onUpdate));
        _onUpdate = callback;
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    public void EnsureCapacity(int capacity)
    {
        if (capacity <= _data.Length) return;

        var newCap = CapacityUtils.CapacityGrowthToFit(_data.Length, capacity);
        if (newCap > GfxLimits.StoreLimit)
            Throwers.BufferOverflow(typeof(GfxStore<TMeta>).Name, newCap, GfxLimits.StoreLimit);

        GfxLog.Event(new LogEvent(0, 0, newCap, 0, 0, 0, LogTopic.ArrayBuffer, LogScope.Gfx, LogAction.Resize,
            LogLevel.Warn));

        _data.Resize(newCap, true);
    }

    private int AllocateNext()
    {
        var index = SlotHelper.NextSlot(_free, Count);
        if (index >= 0) return index;

        if (Count >= Capacity) EnsureCapacity(1);
        return Count++;
    }

    public void Dispose() => _data.Dispose();

    public void FillGfxStoreMeta( out GfxStoreMeta data)
    {
        data.Fk = new CollectionSample(Count, Capacity, GetAliveCount(), FreeCount);
        data.Bk = default;
        data.Kind = GraphicsKind;
        data.MetaInfo = GfxMetrics.GetSpecialMetric(GraphicsKind);
    }

}

