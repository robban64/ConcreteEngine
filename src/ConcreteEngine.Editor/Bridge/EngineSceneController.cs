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

    public abstract SceneObjectView GetSceneObjectView(SceneObjectId id);
    
    public abstract void Select(SceneObjectId id);
    public abstract void Deselect(SceneObjectId id);
    
    public abstract void FetchTransform(SceneObjectId id, ref TransformStable data);
    public abstract void CommitTransform(SceneObjectId id, in TransformStable data);
    public abstract void CommitParticle(SceneObjectId id, ParticleProperty data);

    
}