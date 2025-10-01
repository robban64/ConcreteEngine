namespace ConcreteEngine.Common.Collections;

public sealed class DictionaryRegistry<TKey, TValue> where TKey : notnull
{
    private readonly Dictionary<TKey, TValue> _registry;
    public int Count => _registry.Count;
    public bool IsFrozen { get; private set; }

    public DictionaryRegistry(int initCapacity = 4)
    {
        _registry = new Dictionary<TKey, TValue>(initCapacity);
    }

    // Optional post-freeze callback to finalize/transform data in-place.
    public void Freeze(Action<Dictionary<TKey, TValue>>? onFreeze = null)
    {
        InvalidOpThrower.ThrowIf(IsFrozen);
        IsFrozen = true;
        onFreeze?.Invoke(_registry);
    }

    public DictionaryRegistry<TKey, TValue> Register(TKey key, TValue value)
    {
        InvalidOpThrower.ThrowIf(IsFrozen, nameof(IsFrozen));
        if (!_registry.TryAdd(key, value))
            throw new InvalidOperationException($"Key already registered: {key}");
        return this;
    }

    public bool TryGet(TKey key, out TValue value) => _registry.TryGetValue(key, out value!);

    public TValue GetRequired(TKey key)
    {
        if (_registry.TryGetValue(key, out var v)) return v;
        throw new KeyNotFoundException($"No registration for {key}");
    }

    public void CopyValuesTo(List<TValue> destination)
    {
        InvalidOpThrower.ThrowIfNot(IsFrozen, nameof(IsFrozen));
        var needed = destination.Count + _registry.Count;
        if (destination.Capacity < needed) destination.Capacity = needed;
        foreach (var v in _registry.Values) destination.Add(v);
    }

    public void Reset()
    {
        IsFrozen = false;
        _registry.Clear();
        _registry.TrimExcess();
    }
}