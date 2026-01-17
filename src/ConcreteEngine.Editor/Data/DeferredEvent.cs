using ConcreteEngine.Editor.Core;

namespace ConcreteEngine.Editor.Data;

internal abstract class DeferredEvent(ComponentEvent message)
{
    public readonly ComponentEvent Message = message;
    public abstract void Execute(StateContext state);
}

internal sealed class DeferredEvent<TEvent>(TEvent message, Action<StateContext, TEvent> handler)
    : DeferredEvent(message) where TEvent : ComponentEvent
{
    public new readonly TEvent Message = message;
    public override void Execute(StateContext state) => handler(state, Message);
}