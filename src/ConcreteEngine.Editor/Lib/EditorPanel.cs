using ConcreteEngine.Core.Common.Memory;
using ConcreteEngine.Editor.Core;
using ConcreteEngine.Editor.Data;

namespace ConcreteEngine.Editor.Lib;

internal abstract class EditorPanel(StateEnums id, StateManager state)
{
    public readonly StateEnums Id = id;
    protected readonly StateManager State = state;

    public MemoryBlockPtr Memory;

    public abstract void OnDraw();
    public virtual void OnUpdateDiagnostic() { }

    public virtual void OnCreate(NativeAllocator allocator) { }
    public virtual void OnAttach() { }

    public virtual void OnEnter() { }
    public virtual void OnLeave() { }
}