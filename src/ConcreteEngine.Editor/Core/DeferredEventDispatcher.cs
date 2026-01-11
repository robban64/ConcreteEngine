using ConcreteEngine.Editor.Data;
using ConcreteEngine.Editor.Definitions;

namespace ConcreteEngine.Editor.Core;

internal sealed class DeferredEventDispatcher
{
    private readonly Dictionary<(Type StateType, EventKey Key), Delegate> _handlers = new();
    private readonly Queue<DeferredEvent> _queue = new();

    public void Register<TState, TEvent>(
        EventKey key,
        Action<GlobalContext, TState, TEvent> handler)
        where TState : class
    {
        _handlers[(typeof(TState), key)]
            = ((GlobalContext context, object stateObj, TEvent evt) => handler(context, (TState)stateObj, evt));
    }


    public void Trigger<TEvent>(
        Type stateType,
        EventKey key,
        ModelStateComponent.StateObject stateObj,
        TEvent evt)
    {
        if (!_handlers.TryGetValue((stateType, key), out var del))
            throw new KeyNotFoundException(key.ToString());

        if (del is not Action<GlobalContext, object, TEvent> erased)
        {
            throw new ArgumentException(
                $"Event {key} was triggered with {typeof(TEvent).Name} but handler expects {del.GetType().Name}");
        }

        _queue.Enqueue(stateObj.MakeEvent(key, evt, erased));
    }

    public void Drain(GlobalContext ctx)
    {
        while (_queue.TryDequeue(out var evt))
            evt.Execute(ctx);
    }
}