using ConcreteEngine.Core.Common.Memory;
using ConcreteEngine.Editor.Core;
using ConcreteEngine.Editor.Data;

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