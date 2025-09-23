namespace ConcreteEngine.Common.Collections;

public sealed class FrozenTypeRegistry<TKeyBase, TValue>
{
    private readonly Dictionary<Type, TValue> _registry;

    private bool _frozen;

    public void Freeze()
    {
        InvalidOpThrower.ThrowIf(_frozen);
        _frozen = true;
    }

    public FrozenTypeRegistry(int initCapacity = 4)
    {
        _registry = new Dictionary<Type, TValue>(initCapacity);
    }

    public FrozenTypeRegistry<TKeyBase, TValue> Register<TKey>(TValue value) where TKey : TKeyBase
    {
        InvalidOpThrower.ThrowIf(_frozen, nameof(_frozen));
        if (!_registry!.TryAdd(typeof(TKey), value))
            throw new InvalidOperationException($"Type already registered: {typeof(TKey).FullName}");
        return this;
    }

    public bool TryGet<TKey>(out TValue value) where TKey : TKeyBase
    {
        InvalidOpThrower.ThrowIfNot(_frozen, nameof(_frozen));
        return _registry.TryGetValue(typeof(TKey), out value!);
    }

    public TValue GetRequired<TKey>() where TKey : TKeyBase
    {
        InvalidOpThrower.ThrowIfNot(_frozen, nameof(_frozen));
        if (_registry.TryGetValue(typeof(TKey), out var v)) return v;
        throw new KeyNotFoundException($"No registration for {typeof(TKey).FullName}");
    }
    
    public TValue GetUntyped(TKeyBase key)
    {
        InvalidOpThrower.ThrowIfNot(_frozen, nameof(_frozen));
        if (_registry.TryGetValue(key!.GetType(), out var v)) return v;
        throw new KeyNotFoundException($"No registration for {key.GetType().FullName}");
    }

    public void Reset()
    {
        _frozen = false;
        _registry.Clear();
        _registry.TrimExcess();
    }
}