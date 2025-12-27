using ConcreteEngine.Core.Common.Identity;
using ConcreteEngine.Core.Diagnostics;
using ConcreteEngine.Editor.Bridge;
using ConcreteEngine.Editor.Data;
using ConcreteEngine.Editor.Definitions;
using ConcreteEngine.Editor.Store;
using ConcreteEngine.Editor.Store.Resources;
using ConcreteEngine.Engine.Diagnostics;
using ConcreteEngine.Engine.ECS;
using ConcreteEngine.Engine.ECS.Definitions;
using ConcreteEngine.Engine.ECS.RenderComponent;
using ConcreteEngine.Engine.Worlds;
using ConcreteEngine.Engine.Worlds.Mesh;
using Ecs = ConcreteEngine.Engine.ECS.Ecs;

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

    public List<EditorEntityResource> LoadEntityList()
    {
        const string animationName = "Animation";
        var result = new List<EditorEntityResource>(Ecs.Render.Core.Count);

        foreach (var query in Ecs.Render.CoreQuery())
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

    public void SelectEntity(EditorId entity, ref EditorEntityState state)
    {
        var entityId = _cachedEntity = new RenderEntityId(entity.Identifier);
        var store = Ecs.Render.Stores<SelectionComponent>.Store;
        if (store.Has(entityId)) return;

        Ecs.Render.Stores<SelectionComponent>.Store.Add(entityId, new SelectionComponent());
        var view = Ecs.Render.Core.GetEntityView(entityId);

        state = new EditorEntityState(in view.Transform.Transform, in view.Box.Bounds)
        {
            Model = new EditorId(view.Source.Model, EditorItemType.Model),
            MaterialKey = new EditorId(view.Source.MaterialKey.Value, EditorItemType.MaterialKey)
        };

        if (Ecs.Render.Stores<RenderAnimationComponent>.Store.Has(entityId))
            state.ComponentRef = new EditorId(entity, EditorItemType.Animation);

        if (Ecs.Render.Stores<ParticleComponent>.Store.Has(entityId))
            state.ComponentRef = new EditorId(entity, EditorItemType.Particle);
    }

    public void DeselectEntity(EditorId entity)
    {
        var entityId = new RenderEntityId(entity.Identifier);
        Ecs.Render.Stores<SelectionComponent>.Store.Remove(entityId);
        _cachedEntity = default;
    }

    public void Fetch(EditorId entity, ref EditorEntityState state)
    {
        if (entity == 0) return;
        var entityId = new RenderEntityId(entity.Identifier);
        var view = Ecs.Render.Core.GetEntityView(entityId);
        state.Transform.Set(in view.Transform.Transform);
        state.Bounds = view.Box.Bounds;
    }

    public void Commit(EditorId entity, in EditorEntityState data)
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

    public void FetchAnimation(EditorId entity, ref EditorAnimationState state)
    {
        var entityId = new RenderEntityId(entity.Identifier);
        ref readonly var component = ref Ecs.Render.Stores<RenderAnimationComponent>.Store.Get(entityId);
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
        ref var component = ref Ecs.Render.Stores<RenderAnimationComponent>.Store.Get(entityId);
        component.Clip = (short)state.Clip;
        component.Time = state.Time;
        component.Speed = state.Speed;
        component.Duration = state.Duration;
    }

    public void FetchParticle(EditorId entity, ref EditorParticleState state)
    {
        var entityId = new RenderEntityId(entity.Identifier);
        var component = Ecs.Render.Stores<ParticleComponent>.Store.Get(entityId);

        var emitter = _world.Particles.GetEmitter(component.Emitter);
        state.EmitterHandle = new EditorId(emitter.EmitterHandle, EditorItemType.ParticleEmitter);
        state.Definition = emitter.Definition;
        state.EmitterState = emitter.State;
    }

    public void CommitParticle(EditorId entity, in EditorParticleState state)
    {
        var emitter = _world.Particles.GetEmitter(new Handle<ParticleEmitter>(state.EmitterHandle));
        emitter.Definition = state.Definition;
        emitter.State = state.EmitterState;
    }
}