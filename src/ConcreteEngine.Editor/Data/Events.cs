using ConcreteEngine.Core.Engine.Assets;
using ConcreteEngine.Core.Engine.Scene;

namespace ConcreteEngine.Editor.Data;

internal abstract class EditorEvent
{
    public enum EventAction : byte
    {
        Unknown,
        Rename,
        Reload,
    }

}

internal sealed class SceneObjectEvent(EditorEvent.EventAction action, SceneObjectId sceneObject, string? name = null) : EditorEvent
{
    public readonly EventAction Action = action;
    public readonly SceneObjectId SceneObject = sceneObject;
    public readonly string? Name = name;
}

internal sealed class SelectionEvent : EditorEvent
{
    public readonly SceneObjectId? SceneObject;
    public readonly AssetId? Asset;

    public SelectionEvent(AssetId asset) => Asset = asset;
    public SelectionEvent(SceneObjectId sceneObject) => SceneObject = sceneObject;
}

internal sealed class AssetEvent(EditorEvent.EventAction action, AssetId asset, string? name = null)
    : EditorEvent
{
    public readonly EventAction Action = action;
    public readonly AssetId Asset = asset;
    public readonly string? Name = name;
}