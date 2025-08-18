namespace ConcreteEngine.Core.Pipeline;

internal sealed class EventBus : IDisposable
{
    private readonly Dictionary<Type, List<Action<IGameEvent>>> _subscribers = new();


    private readonly List<(Type, Action<IGameEvent>)> _pendingAdd = new(32);
    private readonly List<(Type, Action<IGameEvent>)> _pendingRemove = new(32);


    private bool _isPublishing = false;

    internal void Prepare()
    {
        if (_pendingRemove.Count > 0)
        {
            foreach (var (type, handler) in _pendingRemove)
            {
                Remove(type, handler);
            }

            _pendingRemove.Clear();
        }

        if (_pendingAdd.Count > 0)
        {
            foreach (var (type, handler) in _pendingAdd)
            {
                Add(type, handler);
            }

            _pendingAdd.Clear();
        }
    }

    public void Publish<TEvent>(TEvent evt) where TEvent : class, IGameEvent
    {
        if (!_subscribers.TryGetValue(typeof(TEvent), out var list))
            throw new InvalidOperationException($"No subscribers handler for {typeof(TEvent).Name} registered");

        _isPublishing = true;

        foreach (var handler in list)
        {
            handler(evt);
        }

        _isPublishing = false;
    }

    public IDisposable Subscribe<TEvent>(Action<IGameEvent> handler) where TEvent : IGameEvent
    {
        var type = typeof(TEvent);
        EnqueueAdd(type, handler);
        return new SubscribeEntry(() => EnqueueRemove(type, handler));
    }

    private void EnqueueAdd(Type type, Action<IGameEvent> handler)
    {
        if (!_isPublishing) Add(type, handler);
        else _pendingAdd.Add((type, handler));
    }

    private void EnqueueRemove(Type type, Action<IGameEvent> handler)
    {
        if (!_isPublishing) Remove(type, handler);
        _pendingRemove.Add((type, handler));
    }

    private void Add(Type type, Action<IGameEvent> handler)
    {
        if (!_subscribers.TryGetValue(type, out var list))
        {
            _subscribers[type] = new List<Action<IGameEvent>>(8);
            return;
        }

        list.Add(handler);
    }

    private void Remove(Type type, Action<IGameEvent> handler)
    {
        if (_subscribers.TryGetValue(type, out var list)) list.Remove(handler);
        else throw new InvalidOperationException($"No subscribers handler for {type.Name} registered");
    }

    private sealed class SubscribeEntry : IDisposable
    {
        private readonly Action _d;
        private bool _x;

        public SubscribeEntry(Action d)
        {
            _d = d;
        }

        public void Dispose()
        {
            if (_x) return;
            _d();
            _x = true;
        }
    }

    public void Dispose()
    {
        throw new NotImplementedException();
    }
}