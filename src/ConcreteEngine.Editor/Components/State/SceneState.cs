using ConcreteEngine.Core.Engine.Scene;
using ConcreteEngine.Editor.Bridge;
using ConcreteEngine.Editor.Core;
using ConcreteEngine.Editor.Data;

namespace ConcreteEngine.Editor.Components.State;

internal sealed class SceneState
{
    public SceneObjectId PreviousId;
    public TransformStable Transform;
    public ParticleProperty Particle;
    public AnimationProperty Animation;

    public SelectionManager Selection = null!;

    public ReadOnlySpan<ISceneObject> GetSceneObjectSpan() => EngineController.SceneController.GetSceneObjectSpan();


    public void Fill(ReadOnlySpan<ProxyPropertyEntry> properties)
    {
        var sceneId = Selection.SelectedSceneId;
        foreach (var property in properties)
        {
            switch (property)
            {
                case ProxyPropertyEntry<SpatialProperty> spatial:
                    ref var transform = ref Transform;
                    var prevRotation = PreviousId == sceneId ? transform.EulerAngles : default;
                    TransformStable.From(in spatial.Get().Transform, in prevRotation, out transform);
                    break;
                case ProxyPropertyEntry<ParticleProperty> particle: Particle = particle.Get(); break;
                case ProxyPropertyEntry<AnimationProperty> animation: Animation = animation.Get(); break;
            }
        }
    }
}