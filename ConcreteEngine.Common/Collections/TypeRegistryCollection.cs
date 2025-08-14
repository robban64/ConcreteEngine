using System.Collections;

namespace ConcreteEngine.Common.Collections;

public sealed class TypeRegistryCollection<TValue>(int capacity = 16) : IEnumerable<KeyValuePair<Type, TValue>>
{
    private readonly Dictionary<Type, TValue> _registry = new(capacity);

    public void Register<TKey>(TValue value) where TKey : notnull
    {
        ArgumentNullException.ThrowIfNull(value, nameof(value));
        if(_registry.ContainsKey(typeof(TKey)))
            throw new InvalidOperationException($"TypeRegistryCollection: {typeof(TKey)} already registered");

        _registry.Add(typeof(TKey), value);
    }
    
    public bool TryGet<TKey>(out TValue value) where TKey : notnull
    {
        return _registry.TryGetValue(typeof(TKey), out value);
    }

    public TValue Get<TKey>() where TKey : notnull
    {
        if(!_registry.TryGetValue(typeof(TKey), out TValue value))
            throw new KeyNotFoundException($"TypeRegistryCollection: {typeof(TKey).Name} not registered");
        
        return value;
    }
    
    public TValue Get(Type key)
    {
        if(!_registry.TryGetValue(key, out TValue value))
            throw new KeyNotFoundException($"TypeRegistryCollection: {key.Name} not registered");
        
        return value;
    }

    public void Clear() => _registry.Clear();

    public Dictionary<Type, TValue>.ValueCollection Values => _registry.Values;


    public IEnumerator<KeyValuePair<Type, TValue>> GetEnumerator()
    {
        return _registry.GetEnumerator();
    }

    IEnumerator IEnumerable.GetEnumerator()
    {
        return ((IEnumerable)_registry).GetEnumerator();
    }
}