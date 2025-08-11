using System.Collections;

namespace ConcreteEngine.Common.Collections;

public sealed class TypeRegistryCollection<TValue> : IEnumerable<KeyValuePair<Type, TValue>>
{
    private readonly Dictionary<Type, TValue> _registry = new(16);

    public void Register<TKey>(TValue value)
    {
        ArgumentNullException.ThrowIfNull(value, nameof(value));
        if(_registry.ContainsKey(typeof(TKey)))
            throw new InvalidOperationException($"TypeRegistryCollection: {typeof(TKey)} already registered");

        _registry.Add(typeof(TKey), value);
    }

    public TValue Get<TKey>()
    {
        return Get(typeof(TKey));
    }
    
    public TValue Get(Type key)
    {
        if(!_registry.TryGetValue(key, out TValue? value))
            throw new KeyNotFoundException($"TypeRegistryCollection: {key} not registered");

        return value;
    }


    public IEnumerator<KeyValuePair<Type, TValue>> GetEnumerator()
    {
        return _registry.GetEnumerator();
    }

    IEnumerator IEnumerable.GetEnumerator()
    {
        return ((IEnumerable)_registry).GetEnumerator();
    }
}