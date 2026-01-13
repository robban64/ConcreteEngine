using ConcreteEngine.Core.Engine;
using ConcreteEngine.Core.Engine.Scene;

namespace ConcreteEngine.Editor.Bridge;

public abstract class SceneController
{
    public abstract SceneObjectProxy? GetProxy(SceneObjectId id);

    public abstract ReadOnlySpan<ISceneObject> GetSceneObjectSpan();
    public abstract ISceneObject GetSceneObject(SceneObjectId id);

    public abstract void Select(SceneObjectId id);
    public abstract void Deselect(SceneObjectId id);
}