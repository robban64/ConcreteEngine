using ConcreteEngine.Editor.Core;

namespace ConcreteEngine.Editor.Lib;

internal abstract class EditorPanel(InspectorId id, StateManager state)
{
    public readonly InspectorId Id = id;
    protected readonly StateManager State = state;

    public abstract void OnDraw();
    public virtual void OnUpdateDiagnostic() { }

    public virtual void OnCreate() { }
    public virtual void OnAttach() { }

    public virtual void OnEnter() { }
    public virtual void OnLeave() { }
}