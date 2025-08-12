using System.Runtime.CompilerServices;
using ConcreteEngine.Graphics.Error;

namespace ConcreteEngine.Graphics;

internal sealed class GraphicsResourceStore(Action<IGraphicsResource> removeHandler)
{
    private static int _resourceCounter = 1;
    private readonly IGraphicsResource[] _resources = new IGraphicsResource[128];

    private readonly List<int> _resourceDisposeQueue = [];

    public int Count => _resourceCounter;
    
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public T? Get<T>(int i) where T : class, IGraphicsResource
    {
        var resource = _resources[i - 1];
        if (resource is T t) return t;
        return null;
    }
    
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool TryGet<T>(int i, out T value) where T : class, IGraphicsResource
    {
        var resource = _resources[i - 1];
        if (resource is T t) { value = t; return true; }
        value = null!;
        return false;
    }
    
    public int AddResource<TResource>(TResource resource) where TResource : IGraphicsResource
    {
        var id = _resourceCounter;
        _resources[id - 1] = resource;
        return _resourceCounter++;
    }

    public void EnqueueRemoveResource(int resourceId)
    {
        if (resourceId > _resourceCounter)
            GraphicsException.ThrowCapabilityExceeded<GraphicsResourceStore>("resource counter", resourceId,
                _resourceCounter);

        var resource = _resources[resourceId];
        if (resource == null!)
            GraphicsException.ThrowResourceNotFound(resourceId);

        if (resource.IsDisposed)
            GraphicsException.ThrowResourceIsDisposed(resourceId);

        _resourceDisposeQueue.Add(resourceId);
    }

    public void FlushRemoveQueue()
    {
        if (_resourceDisposeQueue.Count > 0)
        {
            foreach (var resourceId in _resourceDisposeQueue)
            {
                RemoveResource(resourceId);
            }

            _resourceDisposeQueue.Clear();
        }

        _resourceDisposeQueue.Clear();
    }


    private void RemoveResource(int resourceId)
    {
        if (resourceId > _resourceCounter)
            GraphicsException.ThrowCapabilityExceeded<GraphicsResourceStore>("resource counter", resourceId,
                _resourceCounter);

        var resource = _resources[resourceId];
        removeHandler(resource);
        _resources[resourceId] = null!;
    }
}