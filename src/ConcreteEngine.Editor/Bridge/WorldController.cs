using ConcreteEngine.Editor.Data;
using ConcreteEngine.Editor.Panels.State;

namespace ConcreteEngine.Editor.Bridge;

public abstract class WorldController
{
    public abstract void CommitCamera(SlotView<EditorCameraState> slot);
    public abstract void FetchCamera(SlotView<EditorCameraState> slot);

    public abstract void CommitVisualParams(SlotView<EditorVisualState> slot);
    public abstract void FetchVisualParams(SlotView<EditorVisualState> slot);
}