using ConcreteEngine.Core.Engine.Scene;
using ConcreteEngine.Editor.Proxy;

namespace ConcreteEngine.Editor.Controller;

public abstract class SceneController
{
    public abstract SceneObjectProxy? GetProxy(SceneObjectId id);

    public abstract ReadOnlySpan<ISceneObject> GetSceneObjectSpan();
    public abstract ISceneObject GetSceneObject(SceneObjectId id);

    public abstract void Select(SceneObjectId id);
    public abstract void Deselect(SceneObjectId id);
}