using ConcreteEngine.Editor.Core;
using ConcreteEngine.Editor.Definitions;

namespace ConcreteEngine.Editor.Data;

internal class EmptyEvent
{
    public static EmptyEvent Empty { get; } = new();
    private EmptyEvent() { }
}

internal abstract class DeferredEvent(EventKey eventKey, object stateObj)
{
    protected readonly object StateObj = stateObj;
    public readonly EventKey EventKey = eventKey;
    public abstract void Execute(StateContext state);
}

internal sealed class DeferredEvent<TState, TEvent>(
    EventKey eventKey,
    object stateObj,
    TEvent message,
    Action<StateContext, TState, TEvent> handler)
    : DeferredEvent(eventKey, stateObj) where TState : class
{
    public TState State => (TState)StateObj;
    public readonly TEvent Message = message;

    public readonly Action<StateContext, TState, TEvent> Handler = handler;
    public override void Execute(StateContext ctx) => Handler(ctx, State, Message);
}