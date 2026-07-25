using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using ConcreteEngine.Core.Common;
using ConcreteEngine.Core.Common.Collections;
using ConcreteEngine.Core.Common.Memory;
using ConcreteEngine.Core.Diagnostics.Logging;
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

    GfxHandle Remove(GfxId id);
}

internal sealed class GfxResourceStore<TMeta> : IGfxResourceStore where TMeta : unmanaged, IResourceMeta
{
    private NativeSoA<TMeta, GfxHandle> _data;

    private readonly Stack<int> _free;

    private Action<int>? _onUpdate;

    public int Count { get; private set; }
    public GraphicsKind GraphicsKind { get; } = TMeta.ResourceKind;

    internal GfxResourceStore(int initialCapacity)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(initialCapacity, 4);
        ArgumentOutOfRangeException.ThrowIfGreaterThan(initialCapacity, GfxLimits.StoreLimit);

        if (GraphicsKind == GraphicsKind.Invalid) Throwers.InvalidOperation(nameof(GraphicsKind));

        _data = new NativeSoA<TMeta, GfxHandle>(initialCapacity);
        _free = new Stack<int>();
    }

    public int ActiveCount => Count - _free.Count;
    public int FreeCount => _free.Count;
    public int Capacity => _data.Length;

    public ReadOnlySpan<TMeta> GetMetaSpan() => MemoryMarshal.CreateReadOnlySpan(ref _data.At1(0), Count);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public GfxHandle GetHandle(GfxId<TMeta> id)
    {
        return _data.At2(id - 1);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public ref readonly TMeta GetMeta(GfxId<TMeta> id) => ref _data.At1(id - 1);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public GfxHandle GetHandleAndMeta(GfxId<TMeta> id, out TMeta meta)
    {
        var idx = id - 1;
        meta = _data.At1(idx);
        return GetHandle(id);
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
        ArgumentOutOfRangeException.ThrowIfEqual(handle.IsValid(), false, nameof(handle));
        ArgumentOutOfRangeException.ThrowIfZero(handle.Value, nameof(handle));

        var index = AllocateNext();
        _data.At1(index) = meta;
        _data.At2(index) = handle;

        var id = new GfxId<TMeta>((ushort)(index + 1));
        GfxLog.LogGfxStore(id, handle, GraphicsKind.ToLogTopic(), LogAction.Add);
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
        ArgumentOutOfRangeException.ThrowIfEqual(id, 0);
        var index = id - 1;
        var handle = _data.At2(index);
        oldMeta = _data.At1(index);
        _data.At1(index) = default;
        _data.At2(index) = default;

        Count = SlotHelper.FreeSlot(_free, index, Count);

        GfxLog.LogGfxStore(id, handle, GraphicsKind.ToLogTopic(), LogAction.Remove);
        return handle;
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    public GfxId<TMeta> Replace(GfxId<TMeta> id, in TMeta newMeta, GfxHandle newHandle)
    {
        ArgumentOutOfRangeException.ThrowIfEqual(id.Id, 0, nameof(id));
        ArgumentOutOfRangeException.ThrowIfZero(newHandle.Value, nameof(newHandle));

        var index = id - 1;
        _data.At1(index) = newMeta;
        _data.At2(index) = newHandle;

        GfxLog.LogGfxStore(id, newHandle, GraphicsKind.ToLogTopic(), LogAction.Replace);
        _onUpdate?.Invoke(id);
        return id;
    }

    public void ReplaceMeta(GfxId<TMeta> id, in TMeta newMeta, out TMeta oldMeta)
    {
        ArgumentOutOfRangeException.ThrowIfEqual(id, 0);
        int index = id - 1;
        oldMeta = _data.At1(index);
        _data.At1(index) = newMeta;
        _onUpdate?.Invoke(id);
    }

    public int GetAliveCount()
    {
        var count = 0;
        var length = Count;
        for (var i = 0; i < length; i++)
        {
            if (_data.At2(i).IsValid()) count++;
        }

        return count;
    }

    public void BindOnUpdateCallback(Action<int> callback)
    {
        if (_onUpdate is not null) Throwers.InvalidOperation(nameof(_onUpdate));
        _onUpdate = callback;
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    public void EnsureCapacity(int capacity)
    {
        if (capacity <= _data.Length) return;

        var newCap = CapacityUtils.CapacityGrowthToFit(_data.Length, capacity);
        if (newCap > GfxLimits.StoreLimit)
            Throwers.BufferOverflow(typeof(GfxResourceStore<TMeta>).Name, newCap, GfxLimits.StoreLimit);

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

    public void Dispose()
    {
        _data.Dispose();
    }
}