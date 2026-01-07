using ConcreteEngine.Core.Engine;

namespace ConcreteEngine.Editor.Bridge;

public interface IEngineSceneController
{
    ReadOnlySpan<ISceneObject> GetSceneObjectSpan();
    ISceneObject GetSceneObject(SceneObjectId id);
    
    ISceneObject SelectSceneObject(SceneObjectId id);
    ISceneObject DeselectSceneObject(SceneObjectId id);

}