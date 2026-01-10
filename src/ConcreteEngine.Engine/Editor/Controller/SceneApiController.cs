using ConcreteEngine.Core.Common.Identity;
using ConcreteEngine.Core.Common.Numerics;
using ConcreteEngine.Core.Engine;
using ConcreteEngine.Core.Renderer.Data;
using ConcreteEngine.Editor.Bridge;
using ConcreteEngine.Editor.Data;
using ConcreteEngine.Engine.ECS;
using ConcreteEngine.Engine.ECS.RenderComponent;
using ConcreteEngine.Engine.Scene;
using ConcreteEngine.Engine.Worlds.Mesh;

namespace ConcreteEngine.Engine.Editor.Controller;

internal sealed class SceneApiController(ApiContext context) : EngineSceneController
{
    private readonly SceneManager _sceneManager = context.SceneManager;
    private readonly SceneStore _sceneStore = context.SceneManager.Store;


    public override ReadOnlySpan<ISceneObject> GetSceneObjectSpan() => _sceneManager.Store.GetSceneObjectSpan();
    public override ISceneObject GetSceneObject(SceneObjectId id) => _sceneManager.Store.Get(id);

    public override void Select(SceneObjectId id)
    {
        var sceneObject = _sceneManager.Store.Get(id);
        foreach (var entity in sceneObject.GetRenderEntities())
        {
            if (Ecs.Render.Stores<SelectionComponent>.Store.Has(entity)) continue;
            Ecs.Render.Stores<SelectionComponent>.Store.Add(entity, new SelectionComponent());
        }
    }

    public override void Deselect(SceneObjectId id)
    {
        var sceneObject = _sceneManager.Store.Get(id);
        foreach (var entity in sceneObject.GetRenderEntities())
        {
            if (!Ecs.Render.Stores<SelectionComponent>.Store.Has(entity)) continue;
            Ecs.Render.Stores<SelectionComponent>.Store.Remove(entity);
        }
    }

    public override SceneObjectProxy GetProxy(SceneObjectId id)
    {
        var sceneObject = _sceneManager.Store.Get(id);
        var entity = sceneObject.GetRenderEntities()[0]; // wip just to test things
        var props = new List<ProxyPropertyEntry>(4);

        props.Add(ProxyPropertyFactory.CreateSpatialProperty(id));
        props.Add(ProxyPropertyFactory.CreateSourceProperty(entity));

        if (Ecs.Render.Stores<RenderAnimationComponent>.Store.Has(entity))
            props.Add(ProxyPropertyFactory.CreateAnimationProperty(entity));

        if (Ecs.Render.Stores<ParticleComponent>.Store.Has(entity))
            props.Add(ProxyPropertyFactory.CreateParticleProperty(entity));

        return new EditorSceneObjectProxy(sceneObject) { Properties = props };
    }
/*
    public override SceneObjectView GetSceneObjectView(SceneObjectId id)
    {
        var sceneObject = _sceneManager.Store.Get(id);
        var entity = sceneObject.GetRenderEntities()[0]; // wip just to test things
        var props = new List<ISceneObjectProperty>(4);

        var source = Ecs.Render.Core.GetSource(entity);
        var materials = context.World.MaterialTable.GetMaterialTag(source.MaterialKey).AsReadOnlySpan().ToArray();
        props.Add(new SceneObjectProperty<SourceProperty>(new SourceProperty(source.Model, materials)));

        var animationPtr = Ecs.Render.Stores<RenderAnimationComponent>.Store.TryGet(entity);
        var particlePtr = Ecs.Render.Stores<ParticleComponent>.Store.TryGet(entity);

        if (!animationPtr.IsNull)
        {
            var it = animationPtr.Value;
            var value = new AnimationProperty(it.Animation, it.Clip)
            {
                Time = it.Time, Speed = it.Speed, Duration = it.Duration
            };
            props.Add(new SceneObjectProperty<AnimationProperty>(value));
        }

        if (!particlePtr.IsNull)
        {
            var it = particlePtr.Value;
            var emitter = context.World.Particles.GetEmitter(it.Emitter);
            var value = new ParticleProperty(it.Emitter, it.Material, emitter.ParticleCount)
            {
                Definition = emitter.Definition, EmitterState = emitter.State
            };
            props.Add(new SceneObjectProperty<ParticleProperty>(emitter.EmitterName, value));
        }

        return new SceneObjectView(id, sceneObject.GId, sceneObject.Name, sceneObject.Enabled)
        {
            EditTransform = TransformStable.Make(in sceneObject.GetTransform()),
            RenderEcsCount = sceneObject.RenderEntitiesCount,
            GameEcsCount = sceneObject.GameEntitiesCount,
            Properties = props,
        };
    }
*/
   
}