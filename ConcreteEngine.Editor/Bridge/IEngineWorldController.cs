using ConcreteEngine.Editor.Data;
using ConcreteEngine.Editor.Store;
using ConcreteEngine.Shared.Rendering;

namespace ConcreteEngine.Editor.Bridge;

public interface IEngineWorldController
{
    void CommitCamera(EditorSlot<EditorCameraState> slot);
    void FetchCamera(EditorSlot<EditorCameraState> slot);
    void CommitWorldRenderParams(EditorSlot<WorldParamsData> slot);
    void FetchWorldRenderParams(EditorSlot<WorldParamsData> slot);
}