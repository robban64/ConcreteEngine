#region

using ConcreteEngine.Editor.Bridge;
using ConcreteEngine.Editor.Data;
using ConcreteEngine.Editor.Definitions;
using ConcreteEngine.Editor.Store;
using ConcreteEngine.Editor.Store.Resources;
using ConcreteEngine.Engine.Worlds;
using ConcreteEngine.Engine.Worlds.Entities;
using ConcreteEngine.Engine.Worlds.Entities.Components;

#endregion

namespace ConcreteEngine.Engine.Editor.Controller;

internal sealed class EntityApiController : IEngineEntityController
{
    private static readonly string[] SourceNames = Enum.GetNames<RenderSourceKind>();


    private EntityId _cachedEntity;

    private readonly ApiContext _apiContext;
    private readonly World _world;

    public EntityApiController(ApiContext apiContext)
    {
        _apiContext = apiContext;
        _world = _apiContext.World;
    }

    private WorldEntities Entities => _world.Entities;

    public List<EditorEntityResource> CreateEntityList()
    {
        const string animationName = "Animation";
        var entities = Entities;
        var result = new List<EditorEntityResource>(entities.EntityCount);

        foreach (var query in entities.CoreQuery())
        {
            ref readonly var source = ref query.Source;
            var entity = query.Entity;
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
            result[query.CoreIndex].ComponentRef = new EditorId(comp.EmitterHandle, EditorItemType.Particle);
        }

        foreach (var query in entities.Query<AnimationComponent>())
        {
            ref readonly var comp = ref query.Component;
            result[query.CoreIndex].DisplayName = animationName;
            result[query.CoreIndex].ComponentRef = new EditorId(comp.Animation, EditorItemType.Animation);
        }

        return result;
    }

    public void SelectEntity(EditorId entity, out EditorEntityState state)
    {
        var entityId = _cachedEntity = new EntityId(entity.Identifier);
        Entities.ApplyRenderResolverFor(entityId, RenderResolver.Highlight);
        var view = Entities.Core.GetEntityView(entityId);

        state = new EditorEntityState(in Transform.UnsafeAs(ref view.Transform), in view.Box.Bounds)
        {
            Model = new EditorId(view.Source.Model, EditorItemType.Model),
            MaterialKey = new EditorId(view.Source.MaterialKey.Value, EditorItemType.MaterialKey)
        };

        if (Entities.Animations.Has(entityId))
            state.ComponentRef = new EditorId(entity, EditorItemType.Animation);

        if (Entities.Particles.Has(entityId))
            state.ComponentRef = new EditorId(entity, EditorItemType.Particle);
    }

    public void DeselectEntity(EditorId entity)
    {
        var entityId = new EntityId(entity.Identifier);
        Entities.RemoveRenderResolverFor(entityId);
        _cachedEntity = default;
    }

    public void Fetch(EditorId entity, ref EditorEntityState state)
    {
        if (entity == 0) return;
        var entityId = new EntityId(entity.Identifier);
        var view = Entities.Core.GetEntityView(entityId);
        state.Transform.Set(in Transform.UnsafeAs(ref view.Transform));
        state.Bounds = view.Box.Bounds;
    }

    public void Commit(EditorId entity, in EditorEntityState data)
    {
        var entityId = new EntityId(entity.Identifier);
        var view = Entities.Core.GetEntityView(entityId);
        view.Box.Bounds = data.Bounds;
        view.Transform.Translation = data.Transform.Translation;
        view.Transform.Rotation = data.Transform.Rotation;
        view.Transform.Scale = data.Transform.Scale;
    }

    public void FetchAnimation(EditorId entity, ref EditorAnimationState state)
    {
        var entityId = new EntityId(entity.Identifier);
        ref readonly var component = ref Entities.Animations.GetById(entityId);
        var clipCount = _world.GetAnimationTableImpl().GetClipCount(component.Animation);
        state.Animation = new EditorId(component.Animation, EditorItemType.AnimationKey);
        state.Clip = component.Clip;
        state.ClipCount = clipCount;
        state.Time = component.Time;
        state.Speed = (float)component.Speed;
        state.Duration = component.Duration;
    }

    public void CommitAnimation(EditorId entity, in EditorAnimationState state)
    {
        var entityId = new EntityId(entity.Identifier);
        ref var component = ref Entities.Animations.GetById(entityId);
        component.Clip = (short)state.Clip;
        component.Time = state.Time;
        component.Speed = state.Speed;
        component.Duration = state.Duration;
    }

    public void FetchParticle(EditorId entity, ref EditorParticleState state)
    {
        var entityId = new EntityId(entity.Identifier);
        var component = Entities.Particles.GetById(entityId);

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