using ConcreteEngine.Core.Renderer.Visuals;
using ConcreteEngine.Editor.Data;

namespace ConcreteEngine.Editor.Bridge;

public abstract class EngineWorldController
{
    public abstract void CommitCamera(EditorSlot<EditorCameraState> slot);
    public abstract void FetchCamera(EditorSlot<EditorCameraState> slot);
    public abstract void CommitWorldRenderParams(EditorSlot<EditorVisualState> slot);
    public abstract void FetchWorldRenderParams(EditorSlot<EditorVisualState> slot);

}