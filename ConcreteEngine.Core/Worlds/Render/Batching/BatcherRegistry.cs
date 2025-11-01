#region

using ConcreteEngine.Common.Collections;

#endregion

namespace ConcreteEngine.Core.Worlds.Render.Batching;

public sealed class BatcherRegistry
{
    private TypeRegistryCollection<IRenderBatcher> _batches = new();

    public T Register<T>(T t) where T : IRenderBatcher
    {
        _batches.Register<T>(t);
        return t;
    }

    public T Get<T>() where T : IRenderBatcher => _batches.Get<T>();

    //public bool TryGet<T>(out T t) where T : IRenderBatcher => _batches.TryGet<T>(out t);
}