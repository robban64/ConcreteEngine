using ConcreteEngine.Core.Common.Numerics;
using ConcreteEngine.Core.Engine;
using ConcreteEngine.Core.Renderer.Data;
using ConcreteEngine.Editor.Data;

namespace ConcreteEngine.Editor.Bridge;

public interface IEngineSceneController
{
    ReadOnlySpan<ISceneObject> GetSceneObjectSpan();
    ISceneObject GetSceneObject(SceneObjectId id);
    
    void Select(SceneObjectId id);
    void Deselect(SceneObjectId id);
    
    void FetchTransform(SceneObjectId id, ref TransformStable data);
    void CommitTransform(SceneObjectId id, in TransformStable data);


}