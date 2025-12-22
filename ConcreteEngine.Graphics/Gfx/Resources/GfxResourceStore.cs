using System.Runtime.CompilerServices;
using ConcreteEngine.Common;
using ConcreteEngine.Common.Collections;
using ConcreteEngine.Graphics.Configuration;
using ConcreteEngine.Graphics.Diagnostic;
using ConcreteEngine.Graphics.Gfx.Definitions;
using ConcreteEngine.Graphics.Gfx.Resources.Data;
using ConcreteEngine.Graphics.Gfx.Resources.Handles;
using ConcreteEngine.Shared.Diagnostics;

namespace ConcreteEngine.Graphics.Gfx.Resources.Stores;

public interface IGfxResourceStore
{
    ResourceKind ResourceKind { get; }
    int Count { get; }
    int FreeCount { get; }
    int Capacity { get; }

    int GetAliveCount();
}

internal interface IGfxResourceStore<in TId> : IGfxResourceStore where TId : unmanaged, IResourceId
{
    GfxHandle GetHandleUntyped(TId id);
}

internal interface IGfxMetaResourceStore<TMeta> : IGfxResourceStore where TMeta : unmanaged, IResourceMeta
{
    ReadOnlySpan<TMeta> MetaSpan { get; }
}

internal sealed class GfxResourceStore<TId, TMeta> : IGfxResourceStore<TId>, IGfxMetaResourceStore<TMeta>
    where TId : unmanaged, IResourceId where TMeta : unmanaged, IResourceMeta
{
    public readonly ref struct GfxHandleMeta(GfxRefToken<TId> handle, in TMeta meta)
    {
        public readonly GfxRefToken<TId> Handle = handle;
        public readonly ref readonly TMeta Meta = ref meta;
    }

    private unsafe delegate*<in GfxMetaChanged<TMeta>, void> _changeCallback;

    private int _idx;
    private TMeta[] _meta;
    private GfxHandle[] _handle;

    private readonly Stack<int> _free;

    public ResourceKind ResourceKind => TId.Kind;

    internal GfxResourceStore(int initialCapacity)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(initialCapacity, 4);
        ArgumentOutOfRangeException.ThrowIfGreaterThan(initialCapacity, GfxLimits.StoreLimit);

        InvalidOpThrower.ThrowIf(ResourceKind == ResourceKind.Invalid);

        _meta = new TMeta[initialCapacity];
        _handle = new GfxHandle[initialCapacity];
        _free = new Stack<int>();
    }

    public int Count => _idx;
    public int FreeCount => _free.Count;
    public int Capacity => _handle.Length;

    public ReadOnlySpan<TMeta> MetaSpan => _meta.AsSpan(0, _idx);

    public unsafe void BindOnChangeCallback(delegate*<in GfxMetaChanged<TMeta>, void> callback)
    {
        ArgumentNullException.ThrowIfNull(callback);
        InvalidOpThrower.ThrowIf(_changeCallback != null);
        _changeCallback = callback;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public GfxRefToken<TId> TryGetRef(TId id, out TMeta result)
    {
        if ((uint)id.Value < _idx) return GetRefAndMeta(id, out result);
        result = default;
        return default;

    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public ref readonly TMeta GetMeta(TId id) => ref _meta[id.Value - 1];

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public GfxRefToken<TId> GetRefHandle(TId id) => Unsafe.As<GfxHandle, GfxRefToken<TId>>(ref _handle[id.Value - 1]);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public GfxRefToken<TId> GetRefAndMeta(TId id, out TMeta meta)
    {
        var idx = id.Value - 1;
        meta = _meta[idx];
        return Unsafe.As<GfxHandle, GfxRefToken<TId>>(ref _handle[idx]);
    }

    public GfxHandle GetHandleUntyped(TId id) => _handle[id.Value - 1];

    public TId Add(in TMeta meta, GfxRefToken<TId> addRef)
    {
        ArgumentOutOfRangeException.ThrowIfEqual(addRef.IsValid, false, nameof(addRef));
        ArgumentOutOfRangeException.ThrowIfLessThan(addRef.Slot, 0, nameof(addRef));

        var newRef = new GfxRefToken<TId>(addRef.Slot, 1);
        var idx = _free.Count > 0 ? _free.Pop() : Allocate();
        _meta[idx] = meta;
        _handle[idx] = newRef;
        idx += 1;
        var newId = Unsafe.As<int, TId>(ref idx);

        GfxLog.LogGfxStore(newId.Value, newRef, ResourceKind.ToLogTopic(), LogAction.Add);
        return newId;
    }

    public GfxHandle Remove(TId id)
    {
        ArgumentOutOfRangeException.ThrowIfEqual(id.Value, 0, nameof(id));
        return Remove(id, out _);
    }

    public GfxHandle Remove(TId id, out TMeta oldMeta)
    {
        ArgumentOutOfRangeException.ThrowIfEqual(id.Value, 0, nameof(id));
        var idx = id.Value - 1;
        var handle = _handle[idx];
        oldMeta = _meta[idx];
        _meta[idx] = default!;
        _handle[idx] = default!;
        _free.Push(idx);

        GfxLog.LogGfxStore(id.Value, handle, ResourceKind.ToLogTopic(), LogAction.Remove);
        return handle;
    }

    public TId Replace(TId id, in TMeta newMeta, in GfxRefToken<TId> incRef, out GfxRefToken<TId> oldRef)
    {
        ArgumentOutOfRangeException.ThrowIfEqual(id.Value, 0, nameof(id));

        var idx = id.Value - 1;
        oldRef = _handle[idx];

        var newSlot = incRef.Slot;
        var newGen = (ushort)(oldRef.Gen + 1);
        var newRef = new GfxRefToken<TId>(newSlot, newGen);

        var oldMeta = _meta[idx];
        _meta[idx] = newMeta;
        _handle[idx] = newRef;

        unsafe
        {
            if (_changeCallback != null)
                _changeCallback(new GfxMetaChanged<TMeta>(id.Value, in newMeta, newRef.Gen, true, ResourceKind));
        }

        GfxLog.LogGfxStore(id.Value, newRef, ResourceKind.ToLogTopic(), LogAction.Replace);
        return id;
    }

    public void ReplaceMeta(TId id, in TMeta newMeta, out TMeta oldMeta)
    {
        ArgumentOutOfRangeException.ThrowIfEqual(id.Value, 0, nameof(id));
        int idx = id.Value - 1;
        oldMeta = _meta[idx];
        _meta[idx] = newMeta;

        unsafe
        {
            if (_changeCallback != null)
                _changeCallback(new GfxMetaChanged<TMeta>(id.Value, in newMeta, _handle[idx].Gen, true, ResourceKind));
        }
    }

    private int Allocate()
    {
        var len = _meta.Length;
        if (_idx == len)
        {
            var newCap = Arrays.CapacityGrowthSafe(len, len + 1);
            GfxLog.Event(new LogEvent(0, 0, newCap, 0, 0, 0, LogTopic.ArrayBuffer, LogScope.Gfx, LogAction.Resize,
                LogLevel.Warn));

            if (newCap > GfxLimits.StoreLimit)
                throw new InvalidOperationException("Store limit exceeded");

            Array.Resize(ref _meta, newCap);
            Array.Resize(ref _handle, newCap);
        }

        return _idx++;
    }

    public int GetAliveCount()
    {
        var span = _handle.AsSpan(0, _idx);
        var count = 0;
        foreach (var record in span)
        {
            if (record.IsValid) count++;
        }

        return count;
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