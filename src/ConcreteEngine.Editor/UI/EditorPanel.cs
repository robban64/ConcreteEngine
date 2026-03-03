using ConcreteEngine.Editor.Core;

namespace ConcreteEngine.Editor.UI;

internal abstract class EditorPanel(PanelId id, StateContext context)
{
    public readonly PanelId Id = id;
    protected readonly StateContext Context = context;

    public abstract void Draw(FrameContext ctx);
    public virtual void Update() { }
    public virtual void UpdateDiagnostic() { }
    public virtual void Enter() { }
    public virtual void Leave() { }
}