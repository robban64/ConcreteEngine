using ConcreteEngine.Editor.Controller.Proxy;
using ConcreteEngine.Editor.Data;
using ConcreteEngine.Editor.Lib;

namespace ConcreteEngine.Editor.Controller;

public abstract class WorldController
{
    public abstract EditorCameraProperties GetEditorCameraProperties();
    public abstract void CommitCamera(SlotState<EditorCameraState> slot);
    public abstract void FetchCamera(SlotState<EditorCameraState> slot);

    public abstract void CommitVisualParams(SlotState<EditorVisualState> slot);
    public abstract void FetchVisualParams(SlotState<EditorVisualState> slot);
}