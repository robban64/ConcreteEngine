using System.Diagnostics;
using System.Runtime.CompilerServices;
using ConcreteEngine.Graphics.Error;
using ConcreteEngine.Graphics.Resources;

namespace ConcreteEngine.Graphics.Resources;

public interface IResourceStore
{
    ResourceKind ResourceKind { get; }
    int Count { get; }
}

public interface IResourceStore<in TId> : IResourceStore where TId : unmanaged, IResourceId
{
    ref readonly GfxHandle GetHandle(TId id);

    GfxHandle Remove(TId id);
}

internal sealed class FrontendResourceStore<TId, TMeta> : IResourceStore<TId>
    where TId : unmanaged, IResourceId where TMeta : unmanaged, IResourceMeta
{
    internal readonly MakeIdDelegate<TId> MakeId;

    // sanity check
    private const int HardLimit = 10_000;
    private const int MaxDefaultCapacity = 1024;

    public ResourceKind ResourceKind { get; }

    private int _idx = 0;
    private TMeta[] _meta;
    private GfxHandle[] _handle;

    private readonly Stack<int> _free;

    public int Count => _idx;

    public ReadOnlySpan<TMeta> AsMetaSpan() => _meta;
    internal ReadOnlySpan<GfxHandle> AsHandleSpan() => _handle;

    internal FrontendResourceStore(
        ResourceKind resourceKind,
        int initialCapacity,
        MakeIdDelegate<TId> makeId)
    {
        ArgumentOutOfRangeException.ThrowIfEqual((int)resourceKind, (int)ResourceKind.Invalid);
        ArgumentOutOfRangeException.ThrowIfLessThan(initialCapacity, 4, nameof(initialCapacity));
        ArgumentOutOfRangeException.ThrowIfGreaterThan(initialCapacity, MaxDefaultCapacity, nameof(initialCapacity));
        ArgumentNullException.ThrowIfNull(makeId);

        ResourceKind = resourceKind;
        MakeId = makeId;

        _meta = new TMeta[initialCapacity];
        _handle = new GfxHandle[initialCapacity];
        _free = new Stack<int>();
    }

    public bool TryGetHandle(TId id, out GfxHandle handle)
    {
        if (id.Value == 0)
        {
            handle = default;
            return false;
        }

        handle = GetHandle(id);
        return true;
    }

    public bool TryGet(TId id, out GfxHandle handle, out TMeta meta)
    {
        if (id.Value == 0)
        {
            handle = default;
            meta = default;
            return false;
        }

        handle = GetHandleAndMeta(id, out meta);
        return true;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public ref readonly TMeta GetMeta(TId id) => ref _meta[id.Value - 1];

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public ref readonly GfxHandle GetHandle(TId id) => ref _handle[id.Value - 1];

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public ref readonly GfxHandle GetHandleAndMeta(TId id, out TMeta meta)
    {
        int idx = id.Value - 1;
        meta = _meta[idx];
        return ref _handle[idx];
    }

    public TId Add(in TMeta meta, in GfxRefToken<TId> refToken)
    {
        ArgumentOutOfRangeException.ThrowIfEqual(refToken.Handle.IsValid, false, nameof(refToken));
        int idx = _free.Count > 0 ? _free.Pop() : Allocate();
        _meta[idx] = meta;
        _handle[idx] = refToken.Handle;
        return MakeId(idx);
    }

    public GfxHandle Remove(TId id)
    {
        ArgumentOutOfRangeException.ThrowIfEqual(id.Value, 0, nameof(id));
        return Remove(id, out _);
    }

    public GfxHandle Remove(TId id, out TMeta oldMeta)
    {
        ArgumentOutOfRangeException.ThrowIfEqual(id.Value, 0, nameof(id));
        int idx = id.Value - 1;
        var handle = _handle[idx];
        oldMeta = _meta[idx];
        _meta[idx] = default!;
        _handle[idx] = default!;
        _free.Push(idx);
        return handle;
    }

    public TId Replace(TId id, in TMeta newMeta, in GfxRefToken<TId> newHandleRef, out GfxRefToken<TId> oldHandle)
    {
        ArgumentOutOfRangeException.ThrowIfEqual(id.Value, 0, nameof(id));
        int idx = id.Value - 1;
        oldHandle = new GfxRefToken<TId>(in _handle[idx]);
        _meta[idx] = newMeta;
        _handle[idx] = newHandleRef.Handle;
        return id;
    }

    public TMeta ReplaceMeta(TId id, in TMeta newMeta, out TMeta oldMeta)
    {
        ArgumentOutOfRangeException.ThrowIfEqual(id.Value, 0, nameof(id));
        int idx = id.Value - 1;
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
        private readonly FrontendResourceStore<TId, TMeta> _store;
        internal IdEnumerable(FrontendResourceStore<TId, TMeta> store) => _store = store;
        public ResourceIdEnumerator GetEnumerator() => new(_store);
    }

    internal struct ResourceIdEnumerator
    {
        private readonly FrontendResourceStore<TId, TMeta> _store;
        private readonly GfxHandle[] _handles;
        private readonly int _count;
        private int _i;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal ResourceIdEnumerator(FrontendResourceStore<TId, TMeta> store)
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