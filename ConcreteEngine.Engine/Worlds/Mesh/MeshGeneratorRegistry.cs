using ConcreteEngine.Common.Collections;

namespace ConcreteEngine.Engine.Worlds.Mesh;

public sealed class MeshGeneratorRegistry
{
    private readonly Dictionary<Type, MeshGenerator> _batches = new(4);

    public T Register<T>(T t) where T : MeshGenerator
    {
        _batches.Add(typeof(T), t);
        return t;
    }

    public T Get<T>() where T : MeshGenerator => (T)_batches[typeof(T)];

    public bool TryGet<T>(out T value) where T : MeshGenerator
    {
        value = null!;
        if (!_batches.TryGetValue(typeof(T), out var res) || res is not T t) return false;
        value = t;
        return false;
    }

    //public bool TryGet<T>(out T t) where T : IRenderBatcher => _batches.TryGet<T>(out t);
}