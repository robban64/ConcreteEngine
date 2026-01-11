using ConcreteEngine.Editor.Data;

namespace ConcreteEngine.Editor.Bridge;

public abstract class EngineWorldController
{
    public abstract void CommitCamera(SlotView<EditorCameraState> slot);
    public abstract void FetchCamera(SlotView<EditorCameraState> slot);

    public abstract void CommitVisualParams(SlotView<EditorVisualState> slot);
    public abstract void FetchVisualParams(SlotView<EditorVisualState> slot);

}