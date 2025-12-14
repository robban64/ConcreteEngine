using System.Runtime.CompilerServices;
using ConcreteEngine.Common;
using ConcreteEngine.Common.Collections;
using ConcreteEngine.Graphics.Diagnostic;
using ConcreteEngine.Graphics.Gfx.Definitions;
using ConcreteEngine.Shared.Diagnostics;

namespace ConcreteEngine.Graphics.Gfx.Resources;

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
    private unsafe delegate*<in GfxMetaChanged<TMeta>, void> _changeCallback;

    private static TId MakeId(int idx)
    {
        idx += 1;
        return Unsafe.As<int, TId>(ref idx);
    }

    private int _idx = 0;
    private TMeta[] _meta;
    private GfxHandle[] _handle;

    private readonly Stack<int> _free;

    public ResourceKind ResourceKind => TId.Kind;

    internal GfxResourceStore(int initialCapacity)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(initialCapacity, 4, nameof(initialCapacity));
        ArgumentOutOfRangeException.ThrowIfGreaterThan(initialCapacity, GfxLimits.StoreLimit, nameof(initialCapacity));

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

    public bool TryGetRef(TId id, out GfxRefToken<TId> handle, out TMeta meta)
    {
        if (id.Value == 0)
        {
            handle = default;
            meta = default;
            return false;
        }

        handle = GetRefAndMeta(id, out meta);
        return true;
    }

    public ref readonly TMeta GetMeta(TId id) => ref _meta[id.Value - 1];

    public GfxRefToken<TId> GetRefHandle(TId id) => new(_handle[id.Value - 1]);

    public GfxRefToken<TId> GetRefAndMeta(TId id, out TMeta meta)
    {
        var idx = id.Value - 1;
        meta = _meta[idx];
        return new GfxRefToken<TId>(_handle[idx]);
    }

    public GfxHandle GetHandleUntyped(TId id) => _handle[id.Value - 1];

    public TId Add(in TMeta meta, GfxRefToken<TId> addRef)
    {
        var addHandle = addRef.Handle;
        ArgumentOutOfRangeException.ThrowIfEqual(addHandle.IsValid, false, nameof(addRef));
        ArgumentOutOfRangeException.ThrowIfLessThan(addHandle.Slot, 0, nameof(addRef));

        var newRef = GfxRefToken<TId>.Make(addHandle.Slot, 1);
        var idx = _free.Count > 0 ? _free.Pop() : Allocate();
        _meta[idx] = meta;
        _handle[idx] = newRef;
        var newId = MakeId(idx);

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
        oldRef = new GfxRefToken<TId>(_handle[idx]);

        var newSlot = incRef.Handle.Slot;
        var newGen = (ushort)(oldRef.Handle.Gen + 1);
        var newRef = GfxRefToken<TId>.Make(newSlot, newGen);

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
            get => MakeId(_i);
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
    }
}