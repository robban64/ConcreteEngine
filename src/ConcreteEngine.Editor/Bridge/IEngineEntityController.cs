using ConcreteEngine.Core.Engine;
using ConcreteEngine.Editor.Data;

namespace ConcreteEngine.Editor.Bridge;

public interface IEngineEntityController
{
    void SelectEntity(SceneObjectId id, ref EditorEntityState state);
    void DeselectEntity(SceneObjectId id);

    void Fetch(SceneObjectId id, ref EditorEntityState state);
    void Commit(SceneObjectId id, in EditorEntityState data);

    // Components

    void FetchAnimation(SceneObjectId id, ref EditorAnimationState state);
    void CommitAnimation(SceneObjectId id, in EditorAnimationState state);

    void FetchParticle(SceneObjectId id, ref EditorParticleState state);
    void CommitParticle(SceneObjectId id, in EditorParticleState state);
}