using System.Runtime.CompilerServices;
using ConcreteEngine.Core.Common.Memory;
using ConcreteEngine.Editor.Core;
using ConcreteEngine.Editor.Data;

namespace ConcreteEngine.Editor.UI;

internal abstract unsafe class EditorPanel(PanelId id, StateContext context)
{
    public readonly PanelId Id = id;
    protected readonly StateContext Context = context;

    protected MemoryBlockPtr PanelMemory;

    protected NativeView<byte> DataPtr
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => PanelMemory.DataPtr;
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    public virtual void OnCreate() { }

    public virtual void OnEnter() { }
    public virtual void OnLeave() { }

    public abstract void OnDraw(FrameContext ctx);
    public virtual void OnUpdateDiagnostic() { }

    protected ArenaAllocator.ArenaBlockBuilder CreateAllocBuilder()
    {
        if (PanelMemory.Ptr != null)
            throw new InvalidOperationException($"Already allocated for {GetType().Name}");

        return TextBuffers.PersistentArena.AllocBuilder();
    }
}