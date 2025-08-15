using System.Runtime.CompilerServices;
using ConcreteEngine.Graphics.Data;
using ConcreteEngine.Graphics.Error;

namespace ConcreteEngine.Graphics.Resources;

internal sealed class GraphicsResourceStore(Action<IGraphicsResource> removeHandler)
{
    private const int MaxBufferSize = 1024;
    private const int BufferSize = 128;

    private readonly SortedList<ushort, UniformTable> _shaderUniforms = new(8);
    private readonly List<ushort> _resourceDisposeQueue = [];

    private IGraphicsResource[] _resources = new IGraphicsResource[BufferSize];
    private int _capacity = BufferSize;
    private ushort _idx = 1;

    public int Count => _idx;

    public IGraphicsResource[] ToArrayCopy()
    {
        var span = _resources.AsSpan().Slice(0, _idx - 1);
        return span.ToArray();
    }
    
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public IGraphicsResource? Get(ushort i) =>  _resources[i - 1];

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
    
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public UniformTable GetUniformTable(ushort resourceId) 
    {
        var hasResource = _shaderUniforms.TryGetValue(resourceId, out var uniformTable);
        if(!hasResource || uniformTable == null) GraphicsException.ThrowResourceNotFound(resourceId);
        return uniformTable;
    }

    public ushort AddResource<TResource>(TResource resource) where TResource : IGraphicsResource
    {
        if (resource is IShader)
            GraphicsException.ThrowUnsupportedFeature(
                $"Use {nameof(AddShaderResource)} when creating shader resources");
        
        TryGrow();

        var id = _idx;
        _resources[id - 1] = resource;
        return _idx++;
    }

    public ushort AddShaderResource<TShader>(TShader resource, UniformTable uniforms) where TShader : IShader
    {
        TryGrow();
        
        var id = _idx;
        _resources[id - 1] = resource;
        _shaderUniforms.Add(id, uniforms);
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


    private void RemoveResource(ushort resourceId)
    {
        if (resourceId > _idx)
            GraphicsException.ThrowCapabilityExceeded<GraphicsResourceStore>("resource counter", resourceId,
                _idx);

        var resource = _resources[resourceId];
        if (resource is IShader)
        {
            _shaderUniforms.Remove(resourceId);
        }

        removeHandler(resource);
        _resources[resourceId] = null!;
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
}