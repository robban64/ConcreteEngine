using ConcreteEngine.Editor.Core;

namespace ConcreteEngine.Editor.Panels;

internal abstract class EditorPanel(PanelId id, PanelContext context)
{
    public readonly PanelId Id = id;
    protected readonly PanelContext Context = context;

    public abstract void Draw(FrameContext ctx);
    public virtual void Update() { }
    public virtual void UpdateDiagnostic() { }
    public virtual void Enter() { }
    public virtual void Leave() { }
}