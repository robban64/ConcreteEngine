using ConcreteEngine.Editor.Data;

namespace ConcreteEngine.Editor.Core;

internal sealed class EventManager
{
    private readonly Dictionary<Type, EventEntry> _events = new(8);
    private readonly Queue<EventEntry> _queue = new(8);

    public void DrainQueue()
    {
        if (_queue.Count == 0) return;
        while (_queue.TryDequeue(out var entry)) entry.Invoke();
    }

    public void Register<TEvent>(Action<TEvent> handler) where TEvent : EditorEvent
    {
        if (!_events.TryAdd(typeof(TEvent), new EventEntry<TEvent>(handler)))
            throw new InvalidOperationException($"Duplicate event handler: {typeof(TEvent).Name}");
    }

    public void Enqueue<TEvent>(TEvent evt) where TEvent : EditorEvent
    {
        if (!_events.TryGetValue(typeof(TEvent), out var entry))
            throw new KeyNotFoundException(typeof(TEvent).Name);

        if (entry is not EventEntry<TEvent> typedEntry)
        {
            throw new ArgumentException(
                $"Event was triggered with {typeof(TEvent).Name}, expects {entry.GetType().Name}");
        }

        typedEntry.SetContent(evt);
        _queue.Enqueue(entry);
    }

    private abstract class EventEntry
    {
        public abstract void Invoke();
    }

    private sealed class EventEntry<TEvent>(Action<TEvent> handler) : EventEntry
        where TEvent : EditorEvent
    {
        private TEvent? _content;

        public void SetContent(EditorEvent evt) => _content = (TEvent)evt;

        public override void Invoke()
        {
            if (_content is null) throw new InvalidOperationException();
            handler(_content);
            _content = null;
        }
    }
}