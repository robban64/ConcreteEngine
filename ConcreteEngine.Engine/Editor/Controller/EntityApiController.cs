using ConcreteEngine.Editor.Bridge;
using ConcreteEngine.Editor.Data;
using ConcreteEngine.Editor.Definitions;
using ConcreteEngine.Editor.Store;
using ConcreteEngine.Editor.Store.Resources;
using ConcreteEngine.Engine.ECS;
using ConcreteEngine.Engine.ECS.Definitions;
using ConcreteEngine.Engine.ECS.RenderComponent;
using ConcreteEngine.Engine.Worlds;

namespace ConcreteEngine.Engine.Editor.Controller;

internal sealed class EntityApiController : IEngineEntityController
{
    private static readonly string[] SourceNames = Enum.GetNames<EntitySourceKind>();

    private RenderEntityId _cachedEntity;

    private readonly ApiContext _apiContext;
    private readonly World _world;

    public EntityApiController(ApiContext apiContext)
    {
        _apiContext = apiContext;
        _world = _apiContext.World;
    }

    private RenderEntityHub Entities => _world.Entities;

    public List<EditorEntityResource> CreateEntityList()
    {
        const string animationName = "Animation";
        var entities = Entities;
        var result = new List<EditorEntityResource>(entities.EntityCount);

        foreach (var query in entities.CoreQuery())
        {
            ref readonly var source = ref query.Source;
            var entity = query.RenderEntity;
            var item = new EditorEntityResource
            {
                Id = new EditorId(entity, EditorItemType.Entity),
                Generation = 0,
                Name = string.Empty,
                DisplayName = SourceNames[(int)source.Kind],
                Model = new EditorId(source.Model, EditorItemType.Model),
            };
            result.Add(item);
        }

        foreach (var query in entities.Query<ParticleComponent>())
        {
            ref readonly var comp = ref query.Component;
            result[query.RenderEntity - 1].ComponentRef = new EditorId(comp.EmitterHandle, EditorItemType.Particle);
        }

        foreach (var query in entities.Query<AnimationComponent>())
        {
            ref readonly var comp = ref query.Component;
            result[query.RenderEntity - 1].DisplayName = animationName;
            result[query.RenderEntity - 1].ComponentRef = new EditorId(comp.Animation, EditorItemType.Animation);
        }

        return result;
    }

    public void SelectEntity(EditorId entity, ref EditorEntityState state)
    {
        var entityId = _cachedEntity = new RenderEntityId(entity.Identifier);
        var store = Entities.GetStore<SelectionComponent>();
        if (store.Has(entityId)) return;
        
        Entities.AddComponent(entityId, new SelectionComponent());
        var view = Entities.Core.GetEntityView(entityId);

        state = new EditorEntityState(in view.Transform.Transform, in view.Box.Bounds)
        {
            Model = new EditorId(view.Source.Model, EditorItemType.Model),
            MaterialKey = new EditorId(view.Source.MaterialKey.Value, EditorItemType.MaterialKey)
        };

        if (Entities.GetStore<AnimationComponent>().Has(entityId))
            state.ComponentRef = new EditorId(entity, EditorItemType.Animation);

        if (Entities.GetStore<ParticleComponent>().Has(entityId))
            state.ComponentRef = new EditorId(entity, EditorItemType.Particle);
    }

    public void DeselectEntity(EditorId entity)
    {
        var entityId = new RenderEntityId(entity.Identifier);
        Entities.RemoveComponent<SelectionComponent>(entityId);
        _cachedEntity = default;
    }

    public void Fetch(EditorId entity, ref EditorEntityState state)
    {
        if (entity == 0) return;
        var entityId = new RenderEntityId(entity.Identifier);
        var view = Entities.Core.GetEntityView(entityId);
        state.Transform.Set(in view.Transform.Transform);
        state.Bounds = view.Box.Bounds;
    }

    public void Commit(EditorId entity, in EditorEntityState data)
    {
        var entityId = new RenderEntityId(entity.Identifier);
        var view = Entities.Core.GetEntityView(entityId);
        ref var bounds = ref view.Box.Bounds;
        ref var transform = ref view.Transform.Transform;
        bounds = data.Bounds;
        transform.Translation = data.Transform.Translation;
        transform.Rotation = data.Transform.Rotation;
        transform.Scale = data.Transform.Scale;
    }

    public void FetchAnimation(EditorId entity, ref EditorAnimationState state)
    {
        var entityId = new RenderEntityId(entity.Identifier);
        ref readonly var component = ref Entities.GetStore<AnimationComponent>().Get(entityId);
        var clipCount = _world.AnimationTableImpl.GetClipCount(component.Animation);
        state.Animation = new EditorId(component.Animation, EditorItemType.AnimationKey);
        state.Clip = component.Clip;
        state.ClipCount = clipCount;
        state.Time = component.Time;
        state.Speed = (float)component.Speed;
        state.Duration = component.Duration;
    }

    public void CommitAnimation(EditorId entity, in EditorAnimationState state)
    {
        var entityId = new RenderEntityId(entity.Identifier);
        ref var component = ref Entities.GetStore<AnimationComponent>().Get(entityId);
        component.Clip = (short)state.Clip;
        component.Time = state.Time;
        component.Speed = state.Speed;
        component.Duration = state.Duration;
    }

    public void FetchParticle(EditorId entity, ref EditorParticleState state)
    {
        var entityId = new RenderEntityId(entity.Identifier);
        var component = Entities.GetStore<ParticleComponent>().Get(entityId);

        var emitter = _world.Particles.GetEmitter(component.EmitterHandle);
        state.EmitterHandle = new EditorId(emitter.EmitterHandle, EditorItemType.ParticleEmitter);
        state.Definition = emitter.Definition;
        state.EmitterState = emitter.State;
    }

    public void CommitParticle(EditorId entity, in EditorParticleState state)
    {
        var emitter = _world.Particles.GetEmitter(state.EmitterHandle);
        emitter.Definition = state.Definition;
        emitter.State = state.EmitterState;
    }
}