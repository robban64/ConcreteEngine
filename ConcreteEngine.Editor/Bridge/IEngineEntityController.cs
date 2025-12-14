using ConcreteEngine.Editor.Data;
using ConcreteEngine.Editor.Store;

namespace ConcreteEngine.Editor.Bridge;

public interface IEngineEntityController
{
    void SelectEntity(EditorId entity, out EditorEntityState state);
    void DeselectEntity(EditorId entity);

    void Fetch(EditorId entity, ref EditorEntityState state);
    void Commit(EditorId entity, in EditorEntityState data);

    // Components

    void FetchAnimation(EditorId entity, ref EditorAnimationState state);
    void CommitAnimation(EditorId entity, in EditorAnimationState state);

    void FetchParticle(EditorId entity, ref EditorParticleState state);
    void CommitParticle(EditorId entity, in EditorParticleState state);
}