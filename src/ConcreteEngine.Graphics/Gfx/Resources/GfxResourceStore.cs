using System.Runtime.CompilerServices;
using ConcreteEngine.Core.Common;
using ConcreteEngine.Core.Common.Collections;
using ConcreteEngine.Core.Common.Memory;
using ConcreteEngine.Core.Common.Numerics.Maths;
using ConcreteEngine.Core.Diagnostics.Logging;
using ConcreteEngine.Graphics.Configuration;
using ConcreteEngine.Graphics.Diagnostic;
using ConcreteEngine.Graphics.Gfx.Handles;

namespace ConcreteEngine.Graphics.Gfx.Resources;

public interface IGfxResourceStore : IDisposable
{
    GraphicsKind GraphicsKind { get; }
    int Count { get; }
    int FreeCount { get; }
    int Capacity { get; }

    int GetAliveCount();

    void BindOnUpdateCallback(Action<int> callback);
}

internal interface IGfxResourceStore<in TId> : IGfxResourceStore where TId : unmanaged, IResourceId
{
    GfxHandle GetHandle(TId id);
}

internal interface IGfxMetaResourceStore<TMeta> : IGfxResourceStore where TMeta : unmanaged, IResourceMeta
{
    ReadOnlySpan<TMeta> GetMetaSpan();
}

internal sealed class GfxResourceStore<TId, TMeta> : IGfxResourceStore<TId>, IGfxMetaResourceStore<TMeta>
    where TId : unmanaged, IResourceId where TMeta : unmanaged, IResourceMeta
{
    private NativeArray<TMeta> _meta;
    private NativeArray<GfxHandle> _handle;

    private readonly Stack<int> _free;

    private Action<int>? _onUpdate;
    
    public int Count { get; private set; }

    public GraphicsKind GraphicsKind { get; } = TId.Kind;

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
    public GfxHandle GetHandle(TId id) => _handle[id.Value - 1];

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public ref readonly TMeta GetMeta(TId id) => ref _meta[id.Value - 1];

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public GfxHandle GetHandleAndMeta(TId id, out TMeta meta)
    {
        var idx = id.Value - 1;
        meta = _meta[idx];
        return _handle[idx];
    }
    
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public GfxHandle TryGet(TId id, out TMeta result)
    {
        if ((uint)id.Value < (uint)Count) return GetHandleAndMeta(id, out result);
        Unsafe.SkipInit(out result);
        return default;
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    public TId Add(in TMeta meta, GfxHandle addRef)
    {
        ArgumentOutOfRangeException.ThrowIfEqual(addRef.IsValid, false, nameof(addRef));
        ArgumentOutOfRangeException.ThrowIfLessThan(addRef.Slot, 0, nameof(addRef));

        var newRef = new GfxHandle(addRef.Slot, 1, GraphicsKind);
        var idx = _free.Count > 0 ? _free.Pop() : Allocate();
        _meta[idx] = meta;
        _handle[idx] = newRef;
        idx += 1;
        
        var newId = Unsafe.As<int, TId>(ref idx);
        GfxLog.LogGfxStore(newId.Value, newRef, GraphicsKind.ToLogTopic(), LogAction.Add);
        return newId;
    }

    public GfxHandle Remove(TId id)
    {
        ArgumentOutOfRangeException.ThrowIfEqual(id.Value, 0, nameof(id));
        return Remove(id, out _);
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    public GfxHandle Remove(TId id, out TMeta oldMeta)
    {
        ArgumentOutOfRangeException.ThrowIfEqual(id.Value, 0, nameof(id));
        var index = id.Value - 1;
        var handle = _handle[index];
        oldMeta = _meta[index];
        _meta[index] = default!;
        _handle[index] = default!;

        if (index == Count - 1) Count--;
        else _free.Push(index);
        if (ActiveCount == 0 && Count > 0)
        {
            _free.Clear();
            Count = 0;
        }

        GfxLog.LogGfxStore(id.Value, handle, GraphicsKind.ToLogTopic(), LogAction.Remove);
        return handle;
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    public TId Replace(TId id, in TMeta newMeta, in GfxHandle incRef, out GfxHandle oldRef)
    {
        ArgumentOutOfRangeException.ThrowIfEqual(id.Value, 0, nameof(id));

        var idx = id.Value - 1;
        oldRef = _handle[idx];
        var newRef = new GfxHandle(incRef.Slot, (ushort)(oldRef.Gen + 1),GraphicsKind);

        _meta[idx] = newMeta;
        _handle[idx] = newRef;

        GfxLog.LogGfxStore(id.Value, newRef, GraphicsKind.ToLogTopic(), LogAction.Replace);
        _onUpdate?.Invoke(id.Value);
        return id;
    }

    public void ReplaceMeta(TId id, in TMeta newMeta, out TMeta oldMeta)
    {
        ArgumentOutOfRangeException.ThrowIfEqual(id.Value, 0, nameof(id));
        int idx = id.Value - 1;
        oldMeta = _meta[idx];
        _meta[idx] = newMeta;
        _onUpdate?.Invoke(id.Value);
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
        
        var newCap = Arrays.CapacityGrowthSafe(_meta.Length, IntMath.AlignUp(64, capacity));
        if (newCap > GfxLimits.StoreLimit)
            throw new InvalidOperationException("Store limit exceeded");

        GfxLog.Event(new LogEvent(0, 0, newCap, 0, 0, 0, LogTopic.ArrayBuffer, LogScope.Gfx, LogAction.Resize,
            LogLevel.Warn));

        _meta.Resize(newCap, false);
        _handle.Resize(newCap, false);
    }

    private int Allocate()
    {
        var len = _meta.Length;
        if (Count == len)
            EnsureCapacity(len + 1);

        return Count++;
    }

    public void Dispose()
    {
        _meta.Dispose();
        _handle.Dispose();
    }
}