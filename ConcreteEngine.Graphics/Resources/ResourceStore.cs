using System.Diagnostics;
using System.Runtime.CompilerServices;
using ConcreteEngine.Graphics.Error;
using ConcreteEngine.Graphics.Resources;

namespace ConcreteEngine.Graphics.Resources;

public interface IResourceStore
{
    ResourceKind GetResourceKind();
    GfxHandle GetHandleByValue(int idValue);
    GfxHandle Remove(int idValue);

}

public interface IResourceStore<TId> : IResourceStore where TId : unmanaged, IResourceId
{
    TId CreateId(int index);

    ref readonly GfxHandle GetHandle(TId id);

    GfxHandle Remove(TId id);

}

internal sealed class ResourceStore<TId, TMeta> : IResourceStore<TId>
    where TId : unmanaged, IResourceId where TMeta : unmanaged, IResourceMeta
{
    internal readonly MakeIdDelegate<TId> MakeId;

    // sanity check
    private const int HardLimit = 10_000;
    private const int MaxDefaultCapacity = 1024;

    private readonly ResourceKind _resourceKind;

    private int _idx = 0;
    private TMeta[] _meta;
    private GfxHandle[] _handle;

    private readonly Dictionary<uint, (TId, TMeta)> _pendingSlots = new();

    private readonly Stack<int> _free;

    public int Count => _idx;

    public ReadOnlySpan<TMeta> AsMetaSpan() => _meta;
    internal ReadOnlySpan<GfxHandle> AsHandleSpan() => _handle;

    internal ResourceStore(
        ResourceKind resourceKind,
        int initialCapacity,
        MakeIdDelegate<TId> makeId)
    {
        ArgumentOutOfRangeException.ThrowIfEqual((int)resourceKind, (int)ResourceKind.Invalid);
        ArgumentOutOfRangeException.ThrowIfLessThan(initialCapacity, 4, nameof(initialCapacity));
        ArgumentOutOfRangeException.ThrowIfGreaterThan(initialCapacity, MaxDefaultCapacity, nameof(initialCapacity));
        ArgumentNullException.ThrowIfNull(makeId);

        _resourceKind = resourceKind;
        MakeId = makeId;

        _meta = new TMeta[initialCapacity];
        _handle = new GfxHandle[initialCapacity];
        _free = new Stack<int>();
    }

    public ResourceKind GetResourceKind() => _resourceKind;

    public GfxHandle GetHandleByValue(int idValue)
    {
        return _handle[idValue - 1];
    }

    public TId CreateId(int index) => MakeId(index);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public ref readonly TMeta GetMeta(TId id) => ref _meta[id.Id - 1];

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public ref readonly GfxHandle GetHandle(TId id) => ref _handle[id.Id - 1];

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public ref readonly GfxHandle GetHandleAndMeta(TId id, out TMeta meta)
    {
        int idx = id.Id - 1;
        meta = _meta[idx];
        return ref _handle[idx];
    }

    public TId Add(in TMeta meta, in GfxHandle handle)
    {
        int idx = _free.Count > 0 ? _free.Pop() : Allocate();
        _meta[idx] = meta;
        _handle[idx] = handle;
        return MakeId(idx);
    }
    
    public GfxHandle Remove(TId id)
    {
        return Remove(id, out _);
    }

    public GfxHandle Remove(int idValue) => Remove(ResourceTypeConverter.MakeId<TId>(idValue));

    public GfxHandle Remove(TId id, out TMeta oldMeta)
    {
        int idx = id.Id - 1;
        var handle = _handle[idx];
        oldMeta = _meta[idx];

        if (_pendingSlots.TryGetValue(handle.Slot, out var pending))
        {
            var existingHandle = _handle[idx];
            var newHandle = existingHandle with {Gen = (ushort)(existingHandle.Gen + 1)};
            _meta[idx] = pending.Item2;
            _handle[idx] = newHandle;
            _pendingSlots.Remove(handle.Slot);
            
            return newHandle;
        }
        
        _meta[idx] = default!;
        _handle[idx] = default!;
        _free.Push(idx);
        return handle;
    }

    public TId Replace(TId id, in TMeta newMeta, in GfxHandle newHandle, out GfxHandle oldHandle)
    {
        Debug.Assert(id.Id > 0);
        int idx = id.Id - 1;
        oldHandle = _handle[idx];
        _meta[idx] = newMeta;
        _handle[idx] = newHandle;
        return id;
    }

    public TMeta ReplaceMeta(TId id, in TMeta newMeta, out TMeta oldMeta)
    {
        Debug.Assert(id.Id > 0);
        int idx = id.Id - 1;
        oldMeta = _meta[idx];
        _meta[idx] = newMeta;
        return newMeta;
    }

    private int Allocate()
    {
        if (_idx == _meta.Length)
        {
            Debug.Assert(_idx < HardLimit);

            var newCap = _meta.Length * 2;
            Array.Resize(ref _meta, newCap);
            Array.Resize(ref _handle, newCap);
        }

        return _idx++;
    }

    public IdEnumerable IdEnumerator => new(this);

    internal readonly struct IdEnumerable
    {
        private readonly ResourceStore<TId, TMeta> _store;
        internal IdEnumerable(ResourceStore<TId, TMeta> store) => _store = store;
        public ResourceIdEnumerator GetEnumerator() => new(_store);
    }

    internal struct ResourceIdEnumerator
    {
        private readonly ResourceStore<TId, TMeta> _store;
        private readonly GfxHandle[] _handles;
        private readonly int _count;
        private int _i;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal ResourceIdEnumerator(ResourceStore<TId, TMeta> store)
        {
            _store = store;
            _handles = store._handle;
            _count = store._idx;
            _i = -1;
        }

        public TId Current
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => _store.MakeId(_i);
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