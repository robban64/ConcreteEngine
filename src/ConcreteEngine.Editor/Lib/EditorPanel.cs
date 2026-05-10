using ConcreteEngine.Core.Common.Memory;
using ConcreteEngine.Editor.Core;
using ConcreteEngine.Editor.Data;

namespace ConcreteEngine.Editor.Lib;

internal abstract class EditorPanel(StateEnums id, StateManager state)
{
    public readonly StateEnums Id = id;
    protected readonly StateManager State = state;

    public NativeView<byte> DataPtr = NativeView<byte>.MakeNull();

    public abstract void OnDraw();
    public virtual void OnUpdateDiagnostic() { }

    public virtual void OnCreate() { }
    public virtual void OnEnter(NativeAllocator allocator) { }
    public virtual void OnLeave() { }
}