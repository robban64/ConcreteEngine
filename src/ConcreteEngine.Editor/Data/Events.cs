using ConcreteEngine.Core.Engine.Assets;
using ConcreteEngine.Core.Engine.Scene;

namespace ConcreteEngine.Editor.Data;

internal abstract class EditorEvent
{
}

internal sealed class SceneObjectEvent(SceneObjectId sceneObject) : EditorEvent
{
    public readonly SceneObjectId SceneObject = sceneObject;
}

internal sealed class AssetSelectionEvent(AssetId asset) : EditorEvent
{
    public readonly AssetId Asset = asset;
}

internal sealed class AssetUpdateEvent(AssetUpdateEvent.EventAction action, AssetId asset, string? name = null) : EditorEvent
{
    public readonly EventAction Action = action;

    public readonly AssetId Asset = asset;
    
    public readonly string? Name = name;
    
    public enum EventAction
    {
        Reload,
        Rename,
    }
}