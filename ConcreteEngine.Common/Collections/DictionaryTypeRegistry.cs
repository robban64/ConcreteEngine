namespace ConcreteEngine.Common.Collections;

public sealed class DictionaryTypeRegistry<TKeyBase, TValue>
{
    private readonly DictionaryRegistry<Type, TValue> _inner;
    private bool _frozen;

    public int Count => _inner.Count;

    public DictionaryTypeRegistry(int initCapacity = 4)
        => _inner = new DictionaryRegistry<Type, TValue>(initCapacity);

    public void Freeze()
    {
        InvalidOpThrower.ThrowIf(_frozen);
        _frozen = true;
        _inner.Freeze();
    }

    public DictionaryTypeRegistry<TKeyBase, TValue> Register<TKey>(TValue value) where TKey : TKeyBase
    {
        InvalidOpThrower.ThrowIf(_frozen, nameof(_frozen));
        _inner.Register(typeof(TKey), value);
        return this;
    }

    public bool TryGet<TKey>(out TValue value) where TKey : TKeyBase
    {
        InvalidOpThrower.ThrowIfNot(_frozen, nameof(_frozen));
        return _inner.TryGet(typeof(TKey), out value!);
    }

    public TValue GetRequired<TKey>() where TKey : TKeyBase
    {
        InvalidOpThrower.ThrowIfNot(_frozen, nameof(_frozen));
        return _inner.GetRequired(typeof(TKey));
    }

    public TValue GetUntyped(TKeyBase key)
    {
        InvalidOpThrower.ThrowIfNot(_frozen, nameof(_frozen));
        return _inner.GetRequired(key!.GetType());
    }

    public void Reset()
    {
        _frozen = false;
        _inner.Reset();
    }
}
