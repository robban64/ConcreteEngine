using ConcreteEngine.Core.Engine.Scene;
using ConcreteEngine.Editor.Bridge;
using ConcreteEngine.Editor.Data;

namespace ConcreteEngine.Editor.Components.State;

internal sealed class SceneState
{
    public SceneObjectId PreviousId;

    public TransformStable Transform;
    public ParticleProperty Particle;
    public AnimationProperty Animation;

    public SceneObjectProxy? Proxy;
    public SceneObjectId SelectedId => Proxy?.Id ?? SceneObjectId.Empty;

    public ReadOnlySpan<ISceneObject> GetSceneObjectSpan() => EngineController.SceneController.GetSceneObjectSpan();

    public void Fill(ReadOnlySpan<ProxyPropertyEntry> properties)
    {
        foreach (var property in properties)
        {
            switch (property)
            {
                case ProxyPropertyEntry<SpatialProperty> spatial:
                    ref var transform = ref Transform;
                    var prevRotation = PreviousId == SelectedId ? transform.EulerAngles : default;
                    TransformStable.From(in spatial.Get().Transform, in prevRotation, out transform);
                    break;
                case ProxyPropertyEntry<ParticleProperty> particle: Particle = particle.Get(); break;
                case ProxyPropertyEntry<AnimationProperty> animation: Animation = animation.Get(); break;
            }
        }
    }
}