using ConcreteEngine.Editor.Core;
using ConcreteEngine.Editor.Utils;

namespace ConcreteEngine.Editor.UI;

internal abstract class EditorPanel(PanelId id, StateContext context)
{
    public readonly PanelId Id = id;
    protected readonly StateContext Context = context;

    protected ArenaBlock PanelMemory;

    public virtual void OnCreate() { }
    public virtual void OnEnter() { }
    public virtual void OnLeave() { }

    public abstract void OnDraw(FrameContext ctx);
    public virtual void OnUpdateDiagnostic() { }

    protected ArenaBlock AllocatePanelMemory(int bytes)
    {
        if (!PanelMemory.IsNull)
            throw new InvalidOperationException($"Already allocated for {GetType().Name}");

        return PanelMemory = TextBuffers.PersistentArena.Alloc(bytes, true);
    }
}