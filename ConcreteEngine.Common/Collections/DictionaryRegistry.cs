namespace ConcreteEngine.Common.Collections;

public sealed class DictionaryRegistry<TKey, TValue> where TKey : notnull
{
    private readonly Dictionary<TKey, TValue> _registry;
    private bool _frozen;

    public int Count => _registry.Count;

    public DictionaryRegistry(int initCapacity = 4)
        => _registry = new Dictionary<TKey, TValue>(initCapacity);

    public void Freeze()
    {
        InvalidOpThrower.ThrowIf(_frozen);
        _frozen = true;
    }

    public DictionaryRegistry<TKey, TValue> Register(TKey key, TValue value)
    {
        InvalidOpThrower.ThrowIf(_frozen, nameof(_frozen));
        if (!_registry.TryAdd(key, value))
            throw new InvalidOperationException($"Key already registered: {key}");
        return this;
    }

    public bool TryGet(TKey key, out TValue value)
    {
        InvalidOpThrower.ThrowIfNot(_frozen, nameof(_frozen));
        return _registry.TryGetValue(key, out value!);
    }

    public TValue GetRequired(TKey key)
    {
        InvalidOpThrower.ThrowIfNot(_frozen, nameof(_frozen));
        if (_registry.TryGetValue(key, out var v)) return v;
        throw new KeyNotFoundException($"No registration for {key}");
    }

    public void Reset()
    {
        _frozen = false;
        _registry.Clear();
        _registry.TrimExcess();
    }
}