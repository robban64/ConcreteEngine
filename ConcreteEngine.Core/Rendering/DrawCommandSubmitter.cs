using System.Runtime.CompilerServices;
using ConcreteEngine.Common.Collections;
using ConcreteEngine.Graphics.Definitions;

namespace ConcreteEngine.Core.Rendering;

public sealed class DrawCommandSubmitter
{
    private readonly SortedList<RenderTargetId, TypeRegistryCollection<object>> _queueRegistry = new();
    private readonly List<Action> _clearHandlers = new(16);
    
    public void RegisterCommand<T>(RenderTargetId target, int capacity = 32) where T : unmanaged, IDrawCommandMessage
    {
        if (!_queueRegistry.TryGetValue(target, out var registry))
        {
            registry = new TypeRegistryCollection<object>();
            _queueRegistry.Add(target, registry);
        }
        
        var collection = new List<(T cmd, DrawCommandMeta meta)>(capacity);
        registry.Register<T>(collection);
        _clearHandlers.Add(() => collection.Clear());
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void SubmitDraw<T>(in T cmd, in DrawCommandMeta meta) where T : unmanaged, IDrawCommandMessage
    {
        var queue = _queueRegistry[meta.Pass].Get<T>();
        // Direct cast; no boxing. JIT will devirtualize Enqueue.
        ((List<(T cmd, DrawCommandMeta meta)>)queue).Add((cmd, meta));
    }
    
    public List<(T cmd, DrawCommandMeta meta)> GetQueue<T>(RenderTargetId target) where T : unmanaged, IDrawCommandMessage
        => (List<(T cmd, DrawCommandMeta meta)>)_queueRegistry[target].Get<T>();

    public void Clear()
    {
        foreach (var clearHandler in _clearHandlers)
            clearHandler();
        /*
        foreach (var registry in _queueRegistry.Values)
        {
            foreach (var obj in registry.Values)
            {
                ((dynamic)obj).Clear();

            }
            
            //((dynamic)queue.Get()).Clear();
        }
        */
    }

    private interface IDrawCommandSubmitterBucket
    {
        public void Clear();
    }
    private sealed class DrawCommandSubmitterBucket<T>: IDrawCommandSubmitterBucket where T : unmanaged, IDrawCommandMessage
    {
        private readonly TypeRegistryCollection<List<(T cmd, DrawCommandMeta meta)>> _registryBucket;
        private readonly Action _clearHandler;

        public DrawCommandSubmitterBucket()
        {
            _registryBucket = new TypeRegistryCollection<List<(T cmd, DrawCommandMeta meta)>>();
            _clearHandler = () =>
            {
                foreach (var registry in _registryBucket.Values)
                {
                    registry.Clear();
                }
            };
        }

        public void Clear()
        {
            foreach (var registry in _registryBucket.Values)
            {
                registry.Clear();
            }
        }

    }
    /*
    private sealed class RegistryBucket<T> where T : unmanaged, IDrawCommandMessage
    {
        private TypeRegistryCollection<List<(T cmd, DrawCommandMeta meta)>> _registryBucket;
        private Action _clearHandler;

        public RegistryBucket()
        {
            _registryBucket = new TypeRegistryCollection<List<(T cmd, DrawCommandMeta meta)>>();
            _clearHandler = () =>
            {
                _
            }
        }
    }
    */

}