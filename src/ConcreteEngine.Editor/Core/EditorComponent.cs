using ConcreteEngine.Editor.Definitions;

namespace ConcreteEngine.Editor.Core;

internal abstract class EditorComponent<TState> where TState : class, new()
{
    private ComponentRuntime _context = null!;
    public TState State { get; private init; } = null!;

    public virtual void DrawLeft(TState state, in FrameContext ctx) { }
    public virtual void DrawRight(TState state, in FrameContext ctx) { }

    protected void TriggerEvent<TPayload>(EventKey key, TPayload payload) => _context.TriggerEvent(key, payload);

    public static T Make<T>(ComponentRuntime ctx, TState state) where T : EditorComponent<TState>, new()
    {
        return new T { _context = ctx, State = state };
    }
}