using ConcreteEngine.Editor.Core;
using ConcreteEngine.Editor.Core.Definitions;

namespace ConcreteEngine.Editor.Panels;

internal abstract class EditorPanel(PanelId id, PanelContext context)
{
    public readonly PanelId Id = id;
    protected readonly PanelContext Context = context;

    public abstract void Draw(ref FrameContext ctx);
    public virtual void Update() { }
    public virtual void UpdateDiagnostic() { }
    public virtual void Enter() { }
    public virtual void Leave() { }
}