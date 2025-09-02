using ConcreteEngine.Common.Collections;

namespace ConcreteEngine.Core.Rendering.Batchers;

public sealed class BatcherRegistry
{
    private TypeRegistryCollection<IRenderBatcher> _batches = new ();

    internal T Register<T>(T t) where T : IRenderBatcher
    {
        _batches.Register<T>(t);
        return t;
    }
    
    public T Get<T>() where T : IRenderBatcher => _batches.Get<T>();
    
    //public bool TryGet<T>(out T t) where T : IRenderBatcher => _batches.TryGet<T>(out t);

}