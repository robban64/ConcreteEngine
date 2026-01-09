using ConcreteEngine.Core.Common.Numerics;
using ConcreteEngine.Core.Engine;
using ConcreteEngine.Core.Renderer.Data;
using ConcreteEngine.Editor.Data;

namespace ConcreteEngine.Editor.Bridge;

public abstract class EngineSceneController
{
    public abstract SceneObjectProxy GetProxy(SceneObjectId id);
    
    public abstract ReadOnlySpan<ISceneObject> GetSceneObjectSpan();
    public abstract ISceneObject GetSceneObject(SceneObjectId id);
    
    public abstract void Select(SceneObjectId id);
    public abstract void Deselect(SceneObjectId id);

    
}