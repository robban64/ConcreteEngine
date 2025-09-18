using System.Collections.Frozen;

namespace ConcreteEngine.Common.Collections;

public sealed class FrozenTypeRegistry<TKeyBase, TValue>
{
    private Dictionary<Type, TValue>? _activeRegister;
    private FrozenDictionary<Type, TValue> _frozenRegister = null!;

    private bool _frozen;

    public void Freeze()
    {
        InvalidOpThrower.ThrowIfTrue(_frozen);
        _frozen = false;
        _frozenRegister = _activeRegister.ToFrozenDictionary();
        _activeRegister.Clear();
        _activeRegister = null;
    }

    public FrozenTypeRegistry()
    {
        _activeRegister = new Dictionary<Type, TValue>();
    }

    public FrozenTypeRegistry<TKeyBase, TValue> Register<TKey>(TValue value) where TKey : TKeyBase
    {
        InvalidOpThrower.ThrowIfTrue(_frozen, nameof(_frozen));
        if (!_activeRegister!.TryAdd(typeof(TKey), value))
            throw new InvalidOperationException($"Type already registered: {typeof(TKey).FullName}");
        return this;
    }

    public bool TryGet<TKey>(out TValue value) where TKey : TKeyBase
    {
        InvalidOpThrower.ThrowIfFalse(_frozen, nameof(_frozen));
        return _frozenRegister!.TryGetValue(typeof(TKey), out value!);
    }

    public TValue GetRequired<TKey>() where TKey : TKeyBase
    {
        InvalidOpThrower.ThrowIfFalse(_frozen, nameof(_frozen));
        if (_frozenRegister!.TryGetValue(typeof(TKey), out var v)) return v;
        throw new KeyNotFoundException($"No registration for {typeof(TKey).FullName}");
    }
}