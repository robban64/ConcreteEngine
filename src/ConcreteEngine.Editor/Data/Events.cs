using ConcreteEngine.Core.Engine.Assets;
using ConcreteEngine.Core.Engine.Scene;
using ConcreteEngine.Editor.Panels.State;

namespace ConcreteEngine.Editor.Data;

internal abstract class EditorEvent
{
}

internal sealed class SceneObjectEvent(SceneObjectId sceneObject) : EditorEvent
{
    public readonly SceneObjectId SceneObject = sceneObject;
}

internal sealed class AssetEvent(AssetId asset) : EditorEvent
{
    public readonly AssetId Asset = asset;
}

internal sealed class AssetReloadEvent(string name) : EditorEvent
{
    public readonly string Name = name;
}

internal sealed class WorldEvent(SlotState<EditorCameraState> cameraState) : EditorEvent
{
    public readonly SlotState<EditorCameraState> CameraState = cameraState;
}

internal sealed class VisualDataEvent(SlotState<EditorVisualState> state) : EditorEvent
{
    public readonly SlotState<EditorVisualState> State = state;
}

internal sealed class GraphicsSettingsEvent() : EditorEvent
{
    public int? ShadowSize { get; init; }
}