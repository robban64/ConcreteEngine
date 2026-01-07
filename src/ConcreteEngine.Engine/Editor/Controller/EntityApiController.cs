using ConcreteEngine.Core.Common.Identity;
using ConcreteEngine.Core.Engine;
using ConcreteEngine.Editor.Bridge;
using ConcreteEngine.Editor.Data;
using ConcreteEngine.Editor.Definitions;
using ConcreteEngine.Engine.ECS;
using ConcreteEngine.Engine.ECS.RenderComponent;
using ConcreteEngine.Engine.Worlds;
using ConcreteEngine.Engine.Worlds.Mesh;
using Ecs = ConcreteEngine.Engine.ECS.Ecs;

namespace ConcreteEngine.Engine.Editor.Controller;
/*
    public List<EditorEntityResource> LoadEntityList()
    {
        const string animationName = "Animation";
        var result = new List<EditorEntityResource>(Ecs.Render.Core.Count);

        var sourceNames = EnumCache<EntitySourceKind>.NameSpan;
        foreach (var query in Ecs.Render.CoreQuery())
        {
            ref readonly var source = ref query.Source;
            var entity = query.RenderEntity;
            var item = new EditorEntityResource
            {
                Id = new EditorId(entity, EditorItemType.Entity),
                Generation = 0,
                Name = string.Empty,
                DisplayName = sourceNames[(int)source.Kind],
                Model = new EditorId(source.Model, EditorItemType.Model),
            };
            result.Add(item);
        }

        foreach (var query in Ecs.Render.Query<ParticleComponent>())
        {
            ref readonly var comp = ref query.Component;
            result[query.RenderEntity - 1].ComponentRef = new EditorId(comp.Emitter, EditorItemType.Particle);
        }

        foreach (var query in Ecs.Render.Query<RenderAnimationComponent>())
        {
            ref readonly var comp = ref query.Component;
            result[query.RenderEntity - 1].DisplayName = animationName;
            result[query.RenderEntity - 1].ComponentRef = new EditorId(comp.Animation, EditorItemType.Animation);
        }

        Logger.LogString(LogScope.Engine, $"Editor Entities loaded - {result.Count}");
        return result;
    }
*/
internal sealed class EntityApiController : IEngineEntityController
{
    private SceneObjectId _cachedEntity;

    private readonly ApiContext _apiContext;
    private readonly World _world;

    public EntityApiController(ApiContext apiContext)
    {
        _apiContext = apiContext;
        _world = _apiContext.World;
    }

    public void SelectEntity(SceneObjectId id, ref EditorEntityState state)
    {
        _cachedEntity = id;
        var store = Ecs.Render.Stores<SelectionComponent>.Store;
        if (store.Has(id)) return;

        Ecs.Render.Stores<SelectionComponent>.Store.Add(id, new SelectionComponent());
        var view = Ecs.Render.Core.GetEntityView(id);

        state = new EditorEntityState(in view.Transform.Transform, in view.Box.Bounds)
        {
            Model = view.Source.Model,
        };

        if (Ecs.Render.Stores<RenderAnimationComponent>.Store.Has(id))
            state.ComponentRef = entity;

        if (Ecs.Render.Stores<ParticleComponent>.Store.Has(id))
            state.ComponentRef = entity;
    }

    public void DeselectEntity(SceneObjectId id)
    {
        var entityId = new RenderEntityId(entity.Identifier);
        Ecs.Render.Stores<SelectionComponent>.Store.Remove(entityId);
        _cachedEntity = default;
    }

    public void Fetch(SceneObjectId id, ref EditorEntityState state)
    {
        if (entity == 0) return;
        var entityId = new RenderEntityId(entity.Identifier);
        var view = Ecs.Render.Core.GetEntityView(entityId);
        state.Transform.Set(in view.Transform.Transform);
        state.Bounds = view.Box.Bounds;
    }

    public void Commit(SceneObjectId id, in EditorEntityState data)
    {
        var entityId = new RenderEntityId(entity.Identifier);
        var view = Ecs.Render.Core.GetEntityView(entityId);
        ref var bounds = ref view.Box.Bounds;
        ref var transform = ref view.Transform.Transform;
        bounds = data.Bounds;
        transform.Translation = data.Transform.Translation;
        transform.Rotation = data.Transform.Rotation;
        transform.Scale = data.Transform.Scale;
    }

    public void FetchAnimation(SceneObjectId id, ref EditorAnimationState state)
    {
        var entityId = new RenderEntityId(entity.Identifier);
        ref readonly var component = ref Ecs.Render.Stores<RenderAnimationComponent>.Store.Get(entityId);
        var clipCount = _world.AnimationTableImpl.GetClipCount(component.Animation);
        state.Animation = component.Animation;
        state.Clip = component.Clip;
        state.ClipCount = clipCount;
        state.Time = component.Time;
        state.Speed = (float)component.Speed;
        state.Duration = component.Duration;
    }

    public void CommitAnimation(SceneObjectId id, in EditorAnimationState state)
    {
        var entityId = new RenderEntityId(entity.Identifier);
        ref var component = ref Ecs.Render.Stores<RenderAnimationComponent>.Store.Get(entityId);
        component.Clip = (short)state.Clip;
        component.Time = state.Time;
        component.Speed = state.Speed;
        component.Duration = state.Duration;
    }

    public void FetchParticle(SceneObjectId id, ref EditorParticleState state)
    {
        var entityId = new RenderEntityId(entity.Identifier);
        var component = Ecs.Render.Stores<ParticleComponent>.Store.Get(entityId);

        var emitter = _world.Particles.GetEmitter(component.Emitter);
        state.EmitterHandle = emitter.EmitterHandle;
        state.Definition = emitter.Definition;
        state.EmitterState = emitter.State;
    }

    public void CommitParticle(SceneObjectId id, in EditorParticleState state)
    {
        var emitter = _world.Particles.GetEmitter(new Handle<ParticleEmitter>(state.EmitterHandle));
        emitter.Definition = state.Definition;
        emitter.State = state.EmitterState;
    }
}