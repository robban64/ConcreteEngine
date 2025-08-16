using System.Runtime.CompilerServices;
using ConcreteEngine.Graphics.Data;
using ConcreteEngine.Graphics.Error;

namespace ConcreteEngine.Graphics.Resources;

internal sealed class GraphicsResourceStore
{
    private const int MaxBufferSize = 1024;
    private const int BufferSize = 128;

    private int _capacity = BufferSize;

    private ushort _idx = 1;
    private IGraphicsResource[] _resources = new IGraphicsResource[BufferSize];

    public int Count => _idx;

    public IGraphicsResource[] ToArrayCopy()
    {
        var span = _resources.AsSpan().Slice(0, _idx - 1);
        return span.ToArray();
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public IGraphicsResource? Get(ushort i) => _resources[i - 1];

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public T Get<T>(ushort i) where T : class, IGraphicsResource
    {
        var resource = _resources[i - 1];
        if (resource is T t) return t;
        return null;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool TryGet<T>(ushort i, out T value) where T : class, IGraphicsResource
    {
        var resource = _resources[i - 1];
        if (resource is T t)
        {
            value = t;
            return true;
        }

        value = null!;
        return false;
    }


    public ushort AddResource<TResource>(TResource resource) where TResource : class, IGraphicsResource
    {
        TryGrow();
        _resources[_idx - 1] = resource;
        return _idx++;
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
        
        if(previousResource == newResource) GraphicsException.ThrowResourceAlreadyExists(resourceId);
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
}