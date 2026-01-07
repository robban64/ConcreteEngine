using ConcreteEngine.Core.Engine;
using ConcreteEngine.Editor.Data;

namespace ConcreteEngine.Editor.Bridge;

public interface IEngineEntityController
{
    void SelectEntity(SceneObjectId entity, ref EditorEntityState state);
    void DeselectEntity(SceneObjectId entity);

    void Fetch(SceneObjectId entity, ref EditorEntityState state);
    void Commit(SceneObjectId entity, in EditorEntityState data);

    // Components

    void FetchAnimation(SceneObjectId entity, ref EditorAnimationState state);
    void CommitAnimation(SceneObjectId entity, in EditorAnimationState state);

    void FetchParticle(SceneObjectId entity, ref EditorParticleState state);
    void CommitParticle(SceneObjectId entity, in EditorParticleState state);
}