using ConcreteEngine.Core.Engine;

namespace ConcreteEngine.Editor.Bridge;

public abstract class EngineSceneController
{
    public abstract SceneObjectProxy GetProxy(SceneObjectId id);
    
    public abstract ReadOnlySpan<ISceneObject> GetSceneObjectSpan();
    public abstract ISceneObject GetSceneObject(SceneObjectId id);
    
    public abstract void Select(SceneObjectId id);
    public abstract void Deselect(SceneObjectId id);

    
}