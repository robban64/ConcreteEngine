using ConcreteEngine.Core.Engine.Scene;

namespace ConcreteEngine.Editor.Bridge;

public abstract class SceneController
{
    public abstract int Count { get; }
    public abstract int GetCountByKind(SceneObjectKind kind);
    
    public abstract ReadOnlySpan<SceneObject> GetSceneObjectSpan();
    public abstract SceneObject GetSceneObject(SceneObjectId id);
    public abstract bool TryGetAsset(SceneObjectId id, out SceneObject asset);

    public abstract InspectSceneObject Select(SceneObjectId id);
    public abstract void Deselect(SceneObjectId id);
}
