#region

using System.Runtime.CompilerServices;
using ConcreteEngine.Common;
using ConcreteEngine.Common.Collections;
using ConcreteEngine.Graphics.Diagnostic;
using ConcreteEngine.Graphics.Gfx.Definitions;

#endregion

namespace ConcreteEngine.Graphics.Gfx.Resources;

public interface IGfxResourceStore
{
    ResourceKind ResourceKind { get; }
    int Count { get; }
}

internal interface IGfxResourceStore<in TId> : IGfxResourceStore where TId : unmanaged, IResourceId
{
    GfxHandle GetHandleUntyped(TId id);
    GfxHandle Remove(TId id);
}

internal sealed class GfxResourceStore<TId, TMeta> : IGfxResourceStore<TId>
    where TId : unmanaged, IResourceId where TMeta : unmanaged, IResourceMeta
{
    private readonly MakeIdDelegate<TId> _makeId;
    internal GfxMetaChangedDel<TId, TMeta>? ChangeCallback;

    public ResourceKind ResourceKind => TId.Kind;

    private int _idx = 0;
    private TMeta[] _meta;
    private GfxHandle[] _handle;

    private readonly Stack<int> _free;

    public int Count => _idx;

    internal GfxResourceStore(int initialCapacity, MakeIdDelegate<TId> makeId)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(initialCapacity, 4, nameof(initialCapacity));
        ArgumentOutOfRangeException.ThrowIfGreaterThan(initialCapacity, GfxLimits.StoreLimit, nameof(initialCapacity));
        ArgumentNullException.ThrowIfNull(makeId);

        InvalidOpThrower.ThrowIf(ResourceKind == ResourceKind.Invalid);

        _makeId = makeId;

        _meta = new TMeta[initialCapacity];
        _handle = new GfxHandle[initialCapacity];
        _free = new Stack<int>();

    }

    internal void BindOnChangeCallback(GfxMetaChangedDel<TId, TMeta> callback)
    {
        ArgumentNullException.ThrowIfNull(callback);
        InvalidOpThrower.ThrowIf(ChangeCallback != null);
        ChangeCallback = callback;
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

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public ref readonly TMeta GetMeta(TId id) => ref _meta[id.Value - 1];

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public GfxRefToken<TId> GetRefHandle(TId id) => GfxRefToken<TId>.From(in _handle[id.Value - 1]);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public GfxRefToken<TId> GetRefAndMeta(TId id, out TMeta meta)
    {
        var idx = id.Value - 1;
        meta = _meta[idx];
        return GfxRefToken<TId>.From(in _handle[idx]);
    }

    public GfxHandle GetHandleUntyped(TId id) => _handle[id.Value - 1];

    public TId Add(in TMeta meta, in GfxRefToken<TId> refToken)
    {
        ArgumentOutOfRangeException.ThrowIfEqual(refToken.Handle.IsValid, false, nameof(refToken));
        var idx = _free.Count > 0 ? _free.Pop() : Allocate();
        _meta[idx] = meta;
        _handle[idx] = refToken;
        var newId = _makeId(idx);

        UpdateMetrics();
        GfxDebugMetrics.Log(DebugLog.MakeAddGfxStore(newId.Value, refToken));
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

        UpdateMetrics();
        GfxDebugMetrics.Log(DebugLog.MakeRemoveGfxStore(id.Value, handle));
        return handle;
    }

    public TId Replace(TId id, in TMeta newMeta, in GfxRefToken<TId> newHandle, out GfxRefToken<TId> oldHandle)
    {
        ArgumentOutOfRangeException.ThrowIfEqual(id.Value, 0, nameof(id));
        var handle = newHandle.Handle;
        var idx = id.Value - 1;
        oldHandle = new GfxRefToken<TId>(in _handle[idx]);
        var oldMeta = _meta[idx];
        _meta[idx] = newMeta;
        _handle[idx] = handle;

        var message = new GfxMetaChanged<TMeta>(in newMeta, in oldMeta, handle.Gen, true, ResourceKind);
        ChangeCallback?.Invoke(id, in message);
        UpdateMetrics();
        GfxDebugMetrics.Log(DebugLog.MakeReplaceGfxStore(id.Value, handle));

        return id;
    }

    public void ReplaceMeta(TId id, in TMeta newMeta, out TMeta oldMeta)
    {
        ArgumentOutOfRangeException.ThrowIfEqual(id.Value, 0, nameof(id));
        int idx = id.Value - 1;
        oldMeta = _meta[idx];
        _meta[idx] = newMeta;

        var message = new GfxMetaChanged<TMeta>(in newMeta, in oldMeta, _handle[idx].Gen, true, ResourceKind);
        ChangeCallback?.Invoke(id, in message);
    }


    private int Allocate()
    {
        var len = _meta.Length;
        if (_idx == len)
        {
            var newCap = ArrayUtility.CapacityGrowthLinear(len, len * 2, step: 32);
            if (newCap > GfxLimits.StoreLimit)
                throw new InvalidOperationException("Store limit exceeded");

            Array.Resize(ref _meta, newCap);
            Array.Resize(ref _handle, newCap);
        }

        return _idx++;
    }

    private int GetAliveCount()
    {
        var span = _handle.AsSpan(0, _idx);
        var count = 0;
        foreach (var record in span)
        {
            if (record.IsValid) count++;
        }

        return count;
    }

    private void UpdateMetrics()
    {
        GfxDebugMetrics.GetStoreMetrics<TId>().GfxCount = GetAliveCount();
        GfxDebugMetrics.GetStoreMetrics<TId>().GfxFree = _free.Count;
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
            get => _store._makeId(_i);
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