using ConcreteEngine.Core.Common.Memory;
using ConcreteEngine.Core.Engine;
using ConcreteEngine.Core.Engine.Assets;
using ConcreteEngine.Core.Engine.Scene;
using ConcreteEngine.Core.Renderer.Material;
using ConcreteEngine.Editor.Bridge;

namespace ConcreteEngine.Editor.Data;

internal sealed class AssetState
{
    public readonly int AssetKindLength = EnumCache<AssetKind>.Count;
    
    public AssetKind ShowKind;

    public AssetId SelectedId => Proxy?.Asset.Id ?? AssetId.Empty;
    public AssetProxy? Proxy;

    public ReadOnlySpan<IAsset> Assets => EngineController.AssetController.GetAssetSpan(ShowKind);

    public void ResetState()
    {
        ShowKind = default;
        Proxy = null;
    }
}

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