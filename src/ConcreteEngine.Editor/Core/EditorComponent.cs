using ConcreteEngine.Editor.Data;
using ConcreteEngine.Editor.Definitions;

namespace ConcreteEngine.Editor.Core;

internal abstract class EditorComponent<TState> where TState : class, new()
{
    private ModelStateComponent _context = null!;

    public virtual void DrawLeft(TState state){}
    public virtual void DrawRight(TState state){}

    protected void TriggerEvent<TPayload>(EventKey key, TPayload payload) => _context.TriggerEvent(key, payload);

    public static T Make<T>(ModelStateComponent ctx) where T : EditorComponent<TState>, new()
    {
        return new T { _context = ctx };
    }
}