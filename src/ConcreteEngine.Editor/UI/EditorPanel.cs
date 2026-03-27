using System.Runtime.CompilerServices;
using ConcreteEngine.Core.Common.Memory;
using ConcreteEngine.Editor.Core;
using ConcreteEngine.Editor.Utils;

namespace ConcreteEngine.Editor.UI;

internal abstract unsafe class EditorPanel(PanelId id, StateContext context)
{
    public readonly PanelId Id = id;
    protected readonly StateContext Context = context;

    protected ArenaBlock* PanelMemory;

    [MethodImpl(MethodImplOptions.NoInlining)]
    public virtual void OnCreate() { }
    public virtual void OnEnter() { }
    public virtual void OnLeave() { }

    public abstract void OnDraw(FrameContext ctx);
    public virtual void OnUpdateDiagnostic() { }

    [MethodImpl(MethodImplOptions.NoInlining)]
    protected ArenaBlock* AllocatePanelMemory(int bytes)
    {
        if (PanelMemory != null)
            throw new InvalidOperationException($"Already allocated for {GetType().Name}");

        return PanelMemory = TextBuffers.PersistentArena.Alloc(bytes, true);
    }
}