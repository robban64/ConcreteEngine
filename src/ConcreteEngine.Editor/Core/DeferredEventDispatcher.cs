using ConcreteEngine.Editor.Data;
using ConcreteEngine.Editor.Definitions;

namespace ConcreteEngine.Editor.Core;

internal sealed class DeferredEventDispatcher
{
    private readonly Dictionary<(Type EventType, EventKey Key), Delegate> _handlers = new();
    private readonly Queue<DeferredEvent> _queue = new();

    public void Register<TEvent>(
        Type eventType,
        EventKey key,
        Action<StateContext, TEvent> handler)
        where TEvent : ComponentEvent
    {
        _handlers[(eventType, key)] = handler;
    }


    public void EnqueueEvent<TEvent>(ComponentRuntime.StateObject stateObj, TEvent evt)
        where TEvent : ComponentEvent
    {
        var key = evt.EventKey;
        if (!_handlers.TryGetValue((typeof(TEvent), key), out var del))
            throw new KeyNotFoundException(key.ToString());

        if (del is not Action<StateContext, TEvent> handler)
        {
            throw new ArgumentException(
                $"Event {key} was triggered with {typeof(TEvent).Name} but handler expects {del.GetType().Name}");
        }

        //ConsoleGateway.LogPlain($"Event: {evt?.ToString()}");
        _queue.Enqueue(stateObj.MakeEvent(evt, handler));
    }

    public void Drain(StateContext ctx)
    {
        while (_queue.TryDequeue(out var evt)) evt.Execute(ctx);
    }
}