#region

using ConcreteEngine.Common.Collections;

#endregion

namespace ConcreteEngine.Engine.Worlds.MeshGeneration;

public sealed class MeshGeneratorRegistry
{
    private readonly TypeRegistryCollection<IRenderBatcher> _batches = new(4);

    public T Register<T>(T t) where T : IRenderBatcher
    {
        _batches.Register<T>(t);
        return t;
    }

    public T Get<T>() where T : IRenderBatcher => _batches.Get<T>();
    public bool TryGet<T>(out T batcher) where T : class, IRenderBatcher => _batches.TryGet(out batcher);

    //public bool TryGet<T>(out T t) where T : IRenderBatcher => _batches.TryGet<T>(out t);
}