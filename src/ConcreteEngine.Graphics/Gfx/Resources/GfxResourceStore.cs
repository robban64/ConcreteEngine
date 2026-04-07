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

public interface IGfxResourceStore
{
    GraphicsKind GraphicsKind { get; }
    int Count { get; }
    int FreeCount { get; }
    int Length { get; }

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

internal sealed class GfxResourceStore<TId, TMeta> : IDisposable, IGfxResourceStore<TId>, IGfxMetaResourceStore<TMeta>
    where TId : unmanaged, IResourceId where TMeta : unmanaged, IResourceMeta
{
    private int _count;
    private NativeArray<TMeta> _meta;
    private NativeArray<GfxHandle> _handle;

    private readonly Stack<int> _free;

    private Action<int>? _onUpdate;

    public GraphicsKind GraphicsKind => TId.Kind;

    internal GfxResourceStore(int initialCapacity)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(initialCapacity, 4);
        ArgumentOutOfRangeException.ThrowIfGreaterThan(initialCapacity, GfxLimits.StoreLimit);

        InvalidOpThrower.ThrowIf(GraphicsKind == GraphicsKind.Invalid);

        _meta = NativeArray.Allocate<TMeta>(initialCapacity);
        _handle = NativeArray.Allocate<GfxHandle>(initialCapacity);
        _free = new Stack<int>();
    }

    public int Count => _count;
    public int FreeCount => _free.Count;
    public int Length => _handle.Length;

    public ReadOnlySpan<TMeta> GetMetaSpan() => _meta.AsReadOnlySpan(0, _count);


    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public GfxHandle GetHandle(TId id) => _handle[id.Value - 1];

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public ref readonly TMeta GetMeta(TId id) => ref _meta[id.Value - 1];

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public GfxHandle GetHandleAndMeta(TId id, out TMeta meta)
    {
        var idx = id.Value - 1;
        meta = _meta[idx];
        return Unsafe.As<GfxHandle, GfxHandle>(ref _handle[idx]);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public GfxHandle TryGet(TId id, out TMeta result)
    {
        if ((uint)id.Value < (uint)_count) return GetHandleAndMeta(id, out result);
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

    [MethodImpl(MethodImplOptions.NoInlining)]
    public GfxHandle Remove(TId id)
    {
        ArgumentOutOfRangeException.ThrowIfEqual(id.Value, 0, nameof(id));
        return Remove(id, out _);
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    public GfxHandle Remove(TId id, out TMeta oldMeta)
    {
        ArgumentOutOfRangeException.ThrowIfEqual(id.Value, 0, nameof(id));
        var idx = id.Value - 1;
        var handle = _handle[idx];
        oldMeta = _meta[idx];
        _meta[idx] = default!;
        _handle[idx] = default!;
        _free.Push(idx);

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
        for (var i = 0; i < _count; i++)
        {
            if (_handle[i].IsValid) count++;
        }

        return count;
    }


    [MethodImpl(MethodImplOptions.NoInlining)]
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

    [MethodImpl(MethodImplOptions.NoInlining)]
    private int Allocate()
    {
        var len = _meta.Length;
        if (_count == len)
            EnsureCapacity(len + 1);

        return _count++;
    }

    public void Dispose()
    {
        _meta.Dispose();
        _handle.Dispose();
    }


/*
    public IdEnumerable IdEnumerator => new(this);

    public readonly struct IdEnumerable
    {
        private readonly GfxResourceStore<TId, TMeta> _store;
        internal IdEnumerable(GfxResourceStore<TId, TMeta> store) => _store = store;
        public ResourceIdEnumerator GetEnumerator() => new(_store);
    }

    public struct ResourceIdEnumerator
    {
        private readonly GfxResourceStore<TId, TMeta> _store;
        private readonly GfxHandle[] _handles;
        private readonly int _count;
        private int _i;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal ResourceIdEnumerator(GfxResourceStore<TId, TMeta> store)
        {
            _store = store;
            _handles = store._handle;
            _count = store._idx;
            _i = -1;
        }

        public TId Current
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => Unsafe.As<int, TId>(ref _i);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool MoveNext()
        {
            int i = _i;
            var handles = _handles;
            var count = _count;

            while (++i < count)
            {
                if (handles[i].IsValid)
                {
                    _i = i;
                    return true;
                }
            }

            return false;
        }
    }*/
}