using System.Runtime.CompilerServices;
using ConcreteEngine.Core.Common.Numerics;
using ConcreteEngine.Core.Engine.Assets;
using ConcreteEngine.Core.Engine.ECS;
using ConcreteEngine.Core.Engine.Graphics;
using ConcreteEngine.Core.Engine.Scene;
using ConcreteEngine.Editor.Bridge;
using ConcreteEngine.Engine.ECS;
using ConcreteEngine.Engine.ECS.RenderComponent;
using ConcreteEngine.Engine.Scene;
using ConcreteEngine.Engine.Worlds;
using ConcreteEngine.Engine.Worlds.Mesh;

namespace ConcreteEngine.Engine.Editor.Controller;

//TODO remove
internal sealed class ParticleEmitterProxy(ParticleEmitter emitter) : ParticleProxy
{
    public override int ParticleCount => emitter.ParticleCount;
    public override ref ParticleState State => ref emitter.State;
    public override ref ParticleDefinition Definition => ref emitter.Definition;
}

internal sealed class SceneApiController(ApiContext context) : SceneController
{
    private readonly SceneStore _sceneStore = context.SceneManager.Store;
    private readonly World _world = context.World;

    public override int Count => _sceneStore.Count;

    public override void SpawnSceneObject(Model model, in Transform transform) =>
        context.SceneManager.SpawnFrom(model, in transform);

    public override ReadOnlySpan<SceneObject> GetSceneObjectSpan() => _sceneStore.GetSceneObjectSpan();

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public override SceneObject GetSceneObject(SceneObjectId id) => _sceneStore.Get(id);

    public override bool TryGetSceneObject(SceneObjectId id, out SceneObject asset)
        => _sceneStore.TryGet(id, out asset);

    public override int GetCountByKind(SceneObjectKind kind)
    {
        return kind == SceneObjectKind.Empty ? _sceneStore.Count : _sceneStore.GetCountBy(kind);
    }

    public override void ToggleDrawBounds(SceneObjectId id, bool enabled)
    {
        var sceneObject = _sceneStore.Get(id);
        var store = Ecs.Render.Stores<DebugBoundsComponent>.Store;
        foreach (var it in sceneObject.GetRenderEntities())
        {
            var isEnabled = store.Has(it);
            if (isEnabled && !enabled)
            {
                store.Remove(it);
            }
            else if (!isEnabled && enabled)
            {
                store.Add(it, new DebugBoundsComponent());
            }
        }
    }


    public override InspectSceneObject Select(SceneObjectId id)
    {
        var sceneObject = _sceneStore.Get(id);
        var hasDebugBounds = false;
        foreach (var entity in sceneObject.GetRenderEntities())
        {
            if (Ecs.Render.Stores<SelectionComponent>.Store.Has(entity)) continue;
            Ecs.Render.Stores<SelectionComponent>.Store.Add(entity, new SelectionComponent());

            if (Ecs.Render.Stores<DebugBoundsComponent>.Store.Has(entity))
                hasDebugBounds = true;
        }

        ParticleProxy? particleProxy = null;
        if (sceneObject.Kind == SceneObjectKind.Particle)
        {
            foreach (var bp in sceneObject.GetInstances())
            {
                if (bp is ParticleInstance particle)
                {
                    _world.Particles.TryGetEmitter(particle.Blueprint.EmitterName, out var emitter);
                    particleProxy = new ParticleEmitterProxy(emitter);
                }
            }
        }

        return new InspectSceneObject(sceneObject, particleProxy) { ShowDebugBounds = hasDebugBounds};
    }

    public override void Deselect(SceneObjectId id)
    {
        int len = Ecs.Render.Stores<SelectionComponent>.Store.ActiveCount;
        if (len == 0) return;
        Span<RenderEntityId> renderEntities = stackalloc RenderEntityId[len + 1];

        int idx = 0;
        foreach (var query in Ecs.Render.Query<SelectionComponent>())
            renderEntities[idx++] = query.RenderEntity;

        foreach (var it in renderEntities.Slice(0, idx))
            Ecs.Render.Stores<SelectionComponent>.Store.Remove(it);
    }

    /*
        public override SceneObjectProxy GetProxy(SceneObjectId id)
        {
            var sceneObject = _sceneStore.Get(id);
            if (sceneObject == null!) return null!;
            var entity = sceneObject.GetRenderEntities()[0]; // wip just to test things

            AnimationProperty? animation = null;
            if (Ecs.Render.Stores<RenderAnimationComponent>.Store.Has(entity))
                animation = SceneObjectProxyFactory.CreateAnimationProperty(entity);

            ParticleProperty? particle = null;
            if (Ecs.Render.Stores<ParticleComponent>.Store.Has(entity))
                particle = SceneObjectProxyFactory.CreateParticleProperty(entity);

            return new SceneObjectProxy(sceneObject.Id, sceneObject.Name, new SceneProxyProperties
            {
                SourceProperty = SceneObjectProxyFactory.CreateSourceProperty(entity),
                SpatialProperty = SceneObjectProxyFactory.CreateSpatialProperty(id),
                AnimationProperty = animation,
                ParticleProperty = particle,
            });
        }*/
}