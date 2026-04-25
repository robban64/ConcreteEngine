using System.Runtime.CompilerServices;
using ConcreteEngine.Editor.Data;

namespace ConcreteEngine.Editor.Core;

internal sealed class EventManager
{
    private readonly Dictionary<Type, IEventEntry> _eventHandler = new(8);
    private readonly Queue<EditorEvent> _queue = new(8);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void DrainQueue(StateManager ctx)
    {
        if (_queue.Count == 0) return;
        while (_queue.TryDequeue(out var entry))
            _eventHandler[entry.GetType()].Invoke(entry, ctx);
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    public void Register<TEvent>(Action<TEvent, StateManager> dispatch) where TEvent : EditorEvent
    {
        if (!_eventHandler.TryAdd(typeof(TEvent), new EventEntry<TEvent>(dispatch)))
            throw new InvalidOperationException($"Duplicate event handler: {typeof(TEvent).Name}");
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    public void Enqueue(EditorEvent evt)
    {
        if (!_eventHandler.ContainsKey(evt.GetType()))
            throw new KeyNotFoundException(evt.GetType().Name);

        _queue.Enqueue(evt);
    }

    private interface IEventEntry
    {
        void Invoke(EditorEvent evt, StateManager ctx);
    }

    private sealed class EventEntry<TEvent>(Action<TEvent, StateManager> dispatch) : IEventEntry
        where TEvent : EditorEvent
    {
        public void Invoke(EditorEvent evt, StateManager ctx)
        {
            if (evt is not TEvent tEvt)
            {
                throw new ArgumentException
                    ($"Event {evt.GetType().Name} is not of type {typeof(TEvent).Name}", nameof(evt));
            }
            
            Console.WriteLine("Event: " + evt.GetType().Name);
            dispatch(tEvt, ctx);
        }
    }
}