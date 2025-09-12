using System.Diagnostics;
using System.Runtime.CompilerServices;
using ConcreteEngine.Graphics.Resources;

namespace ConcreteEngine.Graphics.Resources;


public interface IResourceStore<in TId, TMeta> 
    where TId : unmanaged, IResourceId where TMeta : unmanaged, IResourceMeta
{
    ref readonly TMeta GetMeta(TId id);
    ReadOnlySpan<TMeta> AsMetaSpan();
    bool IsAlive(TId id);
}

internal sealed class ResourceStore<TId, TMeta> : IResourceStore<TId, TMeta>
    where TId : unmanaged, IResourceId where TMeta : unmanaged, IResourceMeta
{
    internal readonly MakeIdDelegate<TId> MakeId;

    private const int HardLimit = 10_000;
    private const int MaxDefaultCapacity = 1024;
    private const int BufferSize = 128;

    private static EqualityComparer<TMeta> MetaComparer = EqualityComparer<TMeta>.Default;


    private int _idx = 0;
    private TMeta[] _meta;
    private GfxHandle[] _handle;

    private int[] _free;
    private int _freeCount;

    public int Count => _idx;

    public ReadOnlySpan<TMeta> AsMetaSpan() => _meta;
    internal ReadOnlySpan<GfxHandle> AsHandleSpan() => _handle;

    internal ResourceStore(
        int initialCapacity,
        MakeIdDelegate<TId> makeId)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(initialCapacity, 4, nameof(initialCapacity));
        ArgumentOutOfRangeException.ThrowIfGreaterThan(initialCapacity, MaxDefaultCapacity, nameof(initialCapacity));
        ArgumentNullException.ThrowIfNull(makeId);

        MakeId = makeId;

        _meta = new TMeta[initialCapacity];
        _handle = new GfxHandle[initialCapacity];
        _free = new int[initialCapacity];
    }


    public TId Add(in TMeta meta, in GfxHandle handle)
    {
        int idx = _freeCount > 0 ? _free[--_freeCount] : Allocate();
        _meta[idx] = meta;
        _handle[idx] = handle;
        return MakeId(idx + 1);
    }

    public GfxHandle Remove(TId id, out TMeta oldMeta)
    {
        int idx = id.Id - 2;
        oldMeta = _meta[idx];
        var h = _handle[idx];
        _meta[idx] = default!;
        _handle[idx] = default!;
        if (_freeCount == _free.Length) Array.Resize(ref _free, _free.Length * 2);
        _free[_freeCount++] = idx;
        return h;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public ref readonly TMeta GetMeta(TId id) => ref _meta[id.Id - 2];

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public GfxHandle GetHandle(TId id) => _handle[id.Id - 2];

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public GfxHandle GetHandleAndMeta(TId id, out TMeta meta)
    {
        int idx = id.Id - 2;
        meta = _meta[idx];
        return _handle[idx];
    }

    public TId Replace(TId id, in TMeta newMeta, in GfxHandle newHandle, out GfxHandle oldHandle)
    {
        Debug.Assert(id.Id > 0);
        int idx = id.Id - 2;
        oldHandle = _handle[idx];
        _meta[idx] = newMeta;
        _handle[idx] = newHandle;
        return id;
    }
    
    public TMeta ReplaceMeta(TId id, in TMeta newMeta, out TMeta oldMeta)
    {
        Debug.Assert(id.Id > 0);
        int idx = id.Id - 2;
        oldMeta = _meta[idx];
        _meta[idx] = newMeta;
        return newMeta;
    }


    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool IsAlive(TId id) => !MetaComparer.Equals(_meta[id.Id - 2], default);


    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private bool IsAliveAtIndex(int idx) => !MetaComparer.Equals(_meta[idx], default);

    private int Allocate()
    {
        if (_idx == _meta.Length)
        {
            Debug.Assert(_idx < HardLimit);

            var newCap = _meta.Length * 2;
            Array.Resize(ref _meta, newCap);
            Array.Resize(ref _handle, newCap);
            Array.Resize(ref _free, Math.Max(_free.Length, newCap));
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
        private int _i;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal ResourceIdEnumerator(ResourceStore<TId, TMeta> store)
        {
            _store = store;
            _i = -1;
        }

        public TId Current
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => _store.MakeId(_i + 1);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool MoveNext()
        {
            int i = _i;
            while (++i < _store._idx)
            {
                if (_store.IsAliveAtIndex(i))
                {
                    _i = i;
                    return true;
                }
            }

            return false;
        }
    }
}