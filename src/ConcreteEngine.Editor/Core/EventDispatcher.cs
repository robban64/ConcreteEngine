namespace ConcreteEngine.Editor.Core;

internal sealed class EventDispatcher
{
    private readonly Queue<EditorEvent> _queue = new(8);
    private readonly Dictionary<Type, Dispatcher> _eventHandler = new(8);


    public void DrainQueue(StateManager ctx)
    {
        if (_queue.Count == 0) return;
        while (_queue.TryDequeue(out var entry))
            _eventHandler[entry.GetType()].Invoke(entry, ctx);
    }

    public void Register<TEvent>(Action<TEvent, StateManager> dispatch) where TEvent : EditorEvent
    {
        if (!_eventHandler.TryAdd(typeof(TEvent), new Dispatcher<TEvent>(dispatch)))
            throw new ArgumentException($"Duplicate event handler: {typeof(TEvent).Name}");
    }

    public void Enqueue(EditorEvent evt)
    {
        if (!_eventHandler.ContainsKey(evt.GetType()))
            throw new KeyNotFoundException(evt.GetType().Name);

        _queue.Enqueue(evt);
    }

    private abstract class Dispatcher
    {
        public abstract void Invoke(EditorEvent evt, StateManager ctx);
    }

    private sealed class Dispatcher<TEvent>(Action<TEvent, StateManager> dispatch) : Dispatcher
        where TEvent : EditorEvent
    {
        public override void Invoke(EditorEvent evt, StateManager ctx)
        {
            if (evt is not TEvent tEvt)
            {
                throw new ArgumentException
                    ($"Event {evt.GetType().Name} is not of type {typeof(TEvent).Name}", nameof(evt));
            }

            dispatch(tEvt, ctx);
        }
    }
}