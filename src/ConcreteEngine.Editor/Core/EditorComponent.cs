using ConcreteEngine.Editor.Data;

namespace ConcreteEngine.Editor.Core;

internal abstract class EditorComponent
{
    private ComponentRuntime _runtime = null!;
    public virtual void DrawLeft(ref FrameContext ctx) { }
    public virtual void DrawRight(ref FrameContext ctx) { }

    protected void TriggerEvent<TEvent>(TEvent evt) where TEvent : ComponentEvent => _runtime.TriggerEvent(evt);
    
    public void SetRuntime(ComponentRuntime runtime) => _runtime = runtime;
    
}