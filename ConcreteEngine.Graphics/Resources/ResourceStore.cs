using System.Runtime.CompilerServices;
using ConcreteEngine.Graphics.Data;
using ConcreteEngine.Graphics.Error;

namespace ConcreteEngine.Graphics.Resources;

internal sealed class ResourceStore<TId, TMeta, THandle>
    where TId : struct where TMeta : struct where THandle : struct
{
    private const int MaxBufferSize = 1024;
    private const int BufferSize = 128;

    private readonly Func<int, TId> _makeId;
    private readonly Func<TId, int> _toIndex;

    private ushort _idx = 0;
    private TMeta[] _meta;
    private THandle[] _handle;

    private int[] _free;
    private int _freeCount;
    
    public int Count => _idx;

    public ResourceStore(
        int initialCapacity, 
        Func<int, TId> makeId,
        Func<TId, int> toIndex)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(initialCapacity, 4, nameof(initialCapacity));
        ArgumentOutOfRangeException.ThrowIfGreaterThan(initialCapacity, MaxBufferSize, nameof(initialCapacity));
        ArgumentNullException.ThrowIfNull(makeId);
        ArgumentNullException.ThrowIfNull(toIndex);
        
        _makeId = makeId;
        _toIndex = toIndex;

        _meta = new TMeta[initialCapacity];
        _handle = new THandle[initialCapacity];
        _free = new int[initialCapacity];
    }


    public TId Add(in TMeta meta, in THandle handle)
    {
        int idx = (_freeCount > 0) ? _free[--_freeCount] : Allocate();
        _meta[idx] = meta;
        _handle[idx] = handle;
        return _makeId(idx + 1); // expose index+1
    }

    // Remove slot, return the handle for the caller (device) to actually delete later.
    public THandle Remove(TId id, out TMeta oldMeta)
    {
        int idx = _toIndex(id) - 1;
        oldMeta = _meta[idx];
        var h = _handle[idx];
        _meta[idx] = default!;
        _handle[idx] = default!;
        if (_freeCount == _free.Length) Array.Resize(ref _free, _free.Length * 2);
        _free[_freeCount++] = idx;
        return h;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public ref readonly TMeta GetMeta(TId id)
        => ref _meta[_toIndex(id) - 1];

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public THandle GetHandle(TId id)
        => _handle[_toIndex(id) - 1];

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public THandle GetHandleAndMeta(TId id, out TMeta meta)
    {
        int idx = _toIndex(id) - 1;
        meta = _meta[idx];
        return _handle[idx];
    }
    
    public TId Replace(TId id, in TMeta newMeta, in THandle newHandle, out THandle oldHandle)
    {
        int idx = _toIndex(id) - 1;
        oldHandle = _handle[idx];
        _meta[idx] = newMeta;
        _handle[idx] = newHandle;
        return id;
    }
    
    private int Allocate()
    {
        if (_idx == _meta.Length)
        {
            var newCap = _meta.Length * 2;
            Array.Resize(ref _meta, newCap);
            Array.Resize(ref _handle, newCap);
            Array.Resize(ref _free, Math.Max(_free.Length, newCap));
        }
        return _idx++;
    }

/*
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public IGraphicsResource? Get(ushort i) => _resources[i - 1];

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public T Get<T>(ushort i) where T : class, IGraphicsResource
    {
        var resource = _resources[i - 1];
        if (resource is T t) return t;
        return null;
    }

    public ushort AddResource<TResource>(TResource resource) where TResource : class, IGraphicsResource
    {
        TryGrow();
        _resources[_idx - 1] = resource;
        return _idx++;
    }

    private void TryGrow()
    {
        if (_idx < _capacity - 2) return;
        var newCapacity = _capacity + BufferSize;
        if (newCapacity > MaxBufferSize)
            throw new OutOfMemoryException($"Requested capacity {newCapacity} is too large, MAX:{MaxBufferSize}");

        _capacity += BufferSize;
        Array.Resize(ref _resources, _capacity);
    }

    public void EnqueueRemoveResource(ushort resourceId)
    {
        if (resourceId > _idx)
            GraphicsException.ThrowCapabilityExceeded<GraphicsResourceStore>("resource counter", resourceId,
                _idx);

        var resource = _resources[resourceId];
        if (resource == null!)
            GraphicsException.ThrowResourceNotFound(resourceId);

        //debug
        if (resource.IsDisposed)
            GraphicsException.ThrowResourceIsDisposed(resourceId);
    }

    public void ReplaceResource<T>(ushort resourceId, T newResource, out T previousResource)
        where T : class, IGraphicsResource
    {
        ValidateResourceId(resourceId);

        var resource = Get<T>(resourceId);
        _resources[resourceId - 1] = newResource;

        previousResource = resource;

        if (previousResource == newResource) GraphicsException.ThrowResourceAlreadyExists(resourceId);
    }

    private void TryGrow()
    {
        if (_idx < _capacity - 2) return;
        var newCapacity = _capacity + BufferSize;
        if (newCapacity > MaxBufferSize)
            throw new OutOfMemoryException($"Requested capacity {newCapacity} is too large, MAX:{MaxBufferSize}");

        _capacity += BufferSize;
        Array.Resize(ref _resources, _capacity);
    }

    private void ValidateResourceId(ushort resourceId)
    {
        if (resourceId >= _idx)
            throw GraphicsException.CapabilityExceeded<GraphicsResourceStore>("Out of bound resource access",
                resourceId,
                _idx);
    }
    */
}