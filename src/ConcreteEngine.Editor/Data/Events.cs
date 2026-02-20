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

internal sealed class AssetEvent(AssetId asset) : EditorEvent
{
    public readonly AssetId Asset = asset;
}

internal sealed class AssetReloadEvent(string name) : EditorEvent
{
    public readonly string Name = name;
}