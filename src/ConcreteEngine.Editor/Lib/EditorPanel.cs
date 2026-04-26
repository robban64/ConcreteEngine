using ConcreteEngine.Core.Common.Memory;
using ConcreteEngine.Editor.Core;
using ConcreteEngine.Editor.Data;

namespace ConcreteEngine.Editor.Lib;

internal abstract class EditorPanel(PanelId id, StateManager state)
{
    public readonly PanelId Id = id;
    protected readonly StateManager State = state;

    public NativeView<byte> DataPtr = NativeView<byte>.MakeNull();

    public abstract void OnDraw();
    public virtual void OnUpdateDiagnostic() { }

    public virtual void OnCreate() { }
    public virtual void OnEnter(ref MemoryBlockPtr memory) { }
    public virtual void OnLeave() { }

}