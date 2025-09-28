namespace ConcreteEngine.Common.Collections;

public sealed class DictionaryTypeRegistry<TKeyBase, TValue>
{
    private readonly DictionaryRegistry<Type, TValue> _inner;
    public int Count => _inner.Count;
    public bool IsFrozen => _inner.IsFrozen;

    public DictionaryTypeRegistry(int initCapacity = 4) => _inner = new DictionaryRegistry<Type, TValue>(initCapacity);

    public void Freeze(Action<Dictionary<Type, TValue>>? onFreeze = null) => _inner.Freeze(onFreeze);

    public DictionaryTypeRegistry<TKeyBase, TValue> Register<TKey>(TValue value) where TKey : TKeyBase
    {
        InvalidOpThrower.ThrowIf(_inner.IsFrozen, nameof(IsFrozen));
        _inner.Register(typeof(TKey), value);
        return this;
    }

    public bool TryGet<TKey>(out TValue value) where TKey : TKeyBase => _inner.TryGet(typeof(TKey), out value!);
    public TValue GetRequired<TKey>() where TKey : TKeyBase => _inner.GetRequired(typeof(TKey));
    public TValue GetUntyped(TKeyBase key) => _inner.GetRequired(key!.GetType());
    public void CopyValuesTo(List<TValue> destination) => _inner.CopyValuesTo(destination);
    public void Reset() => _inner.Reset();
}