using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using ConcreteEngine.Common.Collections;
using ConcreteEngine.Graphics.Definitions;

namespace ConcreteEngine.Core.Rendering;
/*
public sealed class DrawCommandSubmitter2
{
    private readonly SortedList<RenderTargetId, TypeRegistryCollection<object>> _queueRegistry = new();
    private readonly List<(RenderTargetId, TypeRegistryCollection<object>, Action)> _clearHandlers = new(32);
    
    public void RegisterCommand<T>(RenderTargetId target, int capacity = 8) where T : unmanaged, IDrawCommandMessage
    {
        if (!_queueRegistry.TryGetValue(target, out var registry))
        {
            registry = new TypeRegistryCollection<object>();
            _queueRegistry.Add(target, registry);
        }
        
        var collection = new List<DrawCommandMessage<T>>(capacity);
        registry.Register<T>(collection);
        _clearHandlers.Add((target, registry, () => collection.Clear()));
    }

    public void UnregisterCommand<T>(RenderTargetId target) where T : unmanaged, IDrawCommandMessage
    {
        if (!_queueRegistry.TryGetValue(target, out var registry))
            throw new InvalidOperationException($"RenderTarget {target} is not registered");
        
        if(!registry.TryGet<T>(out var collectionObj))
            throw new InvalidOperationException($"Command {typeof(T).Name} is not registered.");
        
        int foundClearHandlerIndex = -1;
        for (int i =0; i < _clearHandlers.Count; i++)
        {
            var handler = _clearHandlers[i];
            if (handler.Item1 == target && handler.Item2 == registry)
            {
                foundClearHandlerIndex = i;
                break;
            }
        }

        var collection = (List<DrawCommandMessage<T>>)collectionObj;
        collection.RemoveAt(foundClearHandlerIndex);
        if (collection.Count == 0)
            _queueRegistry.Remove(target);

    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void SubmitDraw<T>(in T cmd, in DrawCommandMeta meta) where T : unmanaged, IDrawCommandMessage
    {
        var queue = _queueRegistry[meta.Pass].Get<T>();
        // Direct cast; no boxing. JIT will devirtualize Enqueue.
        ((List<DrawCommandMessage<T>>)queue).Add(new DrawCommandMessage<T>(in cmd, in meta));
    }
    
    public Span<DrawCommandMessage<T>> GetQueue<T>(RenderTargetId target) where T : unmanaged, IDrawCommandMessage
        => CollectionsMarshal.AsSpan((List<DrawCommandMessage<T>>)_queueRegistry[target].Get<T>());

    public void ClearData()
    {
        foreach (var (_,__,clearHandler) in _clearHandlers)
            clearHandler();
        
        foreach (var registry in _queueRegistry.Values)
            foreach (var obj in registry.Values)
                ((dynamic)obj).Clear();
    }

}
*/