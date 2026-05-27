using System.Runtime.CompilerServices;
using ConcreteEngine.Core.Common;
using ConcreteEngine.Core.Common.Collections;
using ConcreteEngine.Core.Common.Memory;
using ConcreteEngine.Core.Diagnostics.Logging;
using ConcreteEngine.Graphics.Configuration;
using ConcreteEngine.Graphics.Diagnostic;
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

    GfxHandle Remove(GfxId id);
}

internal sealed class GfxResourceStore<TMeta> : IGfxResourceStore
    where TMeta : unmanaged, IResourceMeta
{
    private NativeArray<TMeta> _meta;
    private NativeArray<GfxHandle> _handle;

    private readonly Stack<int> _free;

    private Action<int>? _onUpdate;

    public int Count { get; private set; }

    public GraphicsKind GraphicsKind { get; } = TMeta.ResourceKind;

    internal GfxResourceStore(int initialCapacity)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(initialCapacity, 4);
        ArgumentOutOfRangeException.ThrowIfGreaterThan(initialCapacity, GfxLimits.StoreLimit);

        InvalidOpThrower.ThrowIf(GraphicsKind == GraphicsKind.Invalid);

        _meta = NativeArray.Allocate<TMeta>(initialCapacity);
        _handle = NativeArray.Allocate<GfxHandle>(initialCapacity);
        _free = new Stack<int>();
    }

    public int ActiveCount => Count - _free.Count;
    public int FreeCount => _free.Count;
    public int Capacity => _handle.Length;

    public ReadOnlySpan<TMeta> GetMetaSpan() => _meta.AsReadOnlySpan(0, Count);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public GfxHandle GetHandleRaw(int id) => _handle[id - 1];

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public GfxHandle GetHandle(GfxId<TMeta> id) => _handle[(int)id - 1];

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public ref readonly TMeta GetMeta(GfxId<TMeta> id) => ref _meta[(int)id - 1];

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public GfxHandle GetHandleAndMeta(GfxId<TMeta> id, out TMeta meta)
    {
        var idx = (int)id - 1;
        meta = _meta[idx];
        return _handle[idx];
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public GfxHandle TryGet(GfxId<TMeta> id, out TMeta result)
    {
        if (id < (uint)Count) return GetHandleAndMeta(id, out result);
        Unsafe.SkipInit(out result);
        return default;
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    public GfxId<TMeta> Add(in TMeta meta, GfxHandle handle)
    {
        ArgumentOutOfRangeException.ThrowIfEqual(handle.IsValid, false, nameof(handle));
        ArgumentOutOfRangeException.ThrowIfLessThan(handle.Slot, 0, nameof(handle));

        var newHandle = new GfxHandle(handle.Slot, 1, GraphicsKind);

        var index = AllocateNext();
        _meta[index] = meta;
        _handle[index] = newHandle;

        var id = new GfxId<TMeta>((ushort)(index + 1));
        GfxLog.LogGfxStore(id, newHandle, GraphicsKind.ToLogTopic(), LogAction.Add);
        return id;
    }

    public GfxHandle Remove(GfxId id)
    {
        if (!id.IsValid() || id.Kind != GraphicsKind) Throwers.InvalidOperation($"Invalid handle {id}");
        return Remove(new GfxId<TMeta>(id), out _);
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    public GfxHandle Remove(GfxId<TMeta> id, out TMeta oldMeta)
    {
        ArgumentOutOfRangeException.ThrowIfEqual(id, 0, nameof(id));
        var index = (int)id - 1;
        var handle = _handle[index];
        oldMeta = _meta[index];
        _meta[index] = default!;
        _handle[index] = default!;

        Count = SlotHelper.FreeSlot(_free, index, Count);

        GfxLog.LogGfxStore((int)id, handle, GraphicsKind.ToLogTopic(), LogAction.Remove);
        return handle;
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    public GfxId<TMeta> Replace(GfxId<TMeta> id, in TMeta newMeta, in GfxHandle incRef, out GfxHandle oldRef)
    {
        ArgumentOutOfRangeException.ThrowIfEqual(id, 0, nameof(id));

        var idx = (int)id - 1;
        oldRef = _handle[idx];
        var newRef = new GfxHandle(incRef.Slot, (ushort)(oldRef.Gen + 1), GraphicsKind);

        _meta[idx] = newMeta;
        _handle[idx] = newRef;

        GfxLog.LogGfxStore((int)id, newRef, GraphicsKind.ToLogTopic(), LogAction.Replace);
        _onUpdate?.Invoke((int)id);
        return id;
    }

    public void ReplaceMeta(GfxId<TMeta> id, in TMeta newMeta, out TMeta oldMeta)
    {
        ArgumentOutOfRangeException.ThrowIfEqual((int)id, 0, nameof(id));
        int idx = (int)id - 1;
        oldMeta = _meta[idx];
        _meta[idx] = newMeta;
        _onUpdate?.Invoke((int)id);
    }

    public int GetAliveCount()
    {
        var count = 0;
        var length = Count;
        for (var i = 0; i < length; i++)
        {
            if (_handle[i].IsValid) count++;
        }

        return count;
    }

    public void BindOnUpdateCallback(Action<int> callback)
    {
        InvalidOpThrower.ThrowIfNotNull(_onUpdate);
        _onUpdate = callback;
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    public void EnsureCapacity(int capacity)
    {
        if (capacity <= _meta.Length) return;

        var newCap = CapacityUtils.CapacityGrowthToFit(_meta.Length, capacity);
        if (newCap > GfxLimits.StoreLimit)
            Throwers.BufferOverflow(typeof(GfxResourceStore<TMeta>).Name, newCap, GfxLimits.StoreLimit);

        GfxLog.Event(new LogEvent(0, 0, newCap, 0, 0, 0, LogTopic.ArrayBuffer, LogScope.Gfx, LogAction.Resize,
            LogLevel.Warn));

        _meta.Resize(newCap, false);
        _handle.Resize(newCap, false);
    }

    private int AllocateNext()
    {
        var index = SlotHelper.NextSlot(_free, Count);
        if (index >= 0) return index;

        if (Count >= Capacity) EnsureCapacity(1);
        return Count++;
    }

    public void Dispose()
    {
        _meta.Dispose();
        _handle.Dispose();
    }
}