namespace ConcreteEngine.Engine;

internal sealed class EngineEventBus
{
    private readonly Dictionary<Type, List<Delegate>> _subscribers = new();

    public void Subscribe<T>(Action<T> handler)
    {
        if (!_subscribers.TryGetValue(typeof(T), out var list))
            _subscribers[typeof(T)] = list = new List<Delegate>(4);

        list.Add(handler);
    }

    public void Unsubscribe<T>(Action<T> handler)
    {
        if (_subscribers.TryGetValue(typeof(T), out var list))
            list.Remove(handler);
    }

    public void Publish<T>(T evt)
    {
        if (_subscribers.TryGetValue(typeof(T), out var list))
            foreach (var del in list)
                ((Action<T>)del).Invoke(evt);
    }
}