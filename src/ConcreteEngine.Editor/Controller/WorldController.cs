using ConcreteEngine.Editor.Data;
using ConcreteEngine.Editor.Panels.State;

namespace ConcreteEngine.Editor.Controller;

public abstract class WorldController
{
    public abstract void CommitCamera(SlotState<EditorCameraState> slot);
    public abstract void FetchCamera(SlotState<EditorCameraState> slot);

    public abstract void CommitVisualParams(SlotState<EditorVisualState> slot);
    public abstract void FetchVisualParams(SlotState<EditorVisualState> slot);
}