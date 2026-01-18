using ConcreteEngine.Editor.Core;

namespace ConcreteEngine.Editor.Data;

internal abstract class DeferredEvent()
{
    public abstract void Execute(StateContext state);
}

internal sealed class DeferredEvent<TEvent>(TEvent message, Action<StateContext, TEvent> handler)
    : DeferredEvent where TEvent : ComponentEvent
{
    public readonly TEvent Message = message;
    public override void Execute(StateContext state) => handler(state, Message);
}