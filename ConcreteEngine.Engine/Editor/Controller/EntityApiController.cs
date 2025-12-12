#region

using System.Numerics;
using System.Runtime.InteropServices;
using ConcreteEngine.Editor.Bridge;
using ConcreteEngine.Editor.Data;
using ConcreteEngine.Editor.Store;
using ConcreteEngine.Editor.Store.Resources;
using ConcreteEngine.Engine.Worlds;
using ConcreteEngine.Engine.Worlds.Entities;
using ConcreteEngine.Engine.Worlds.Entities.Components;
using ConcreteEngine.Renderer.Data;
using ConcreteEngine.Shared.World;

#endregion

namespace ConcreteEngine.Engine.Editor.Controller;

internal sealed class EntityApiController(ApiContext apiContext) : IEngineEntityController
{
    private static readonly string[] SourceNames = Enum.GetNames<RenderSourceKind>();

    private WorldEntities Entities => apiContext.World.Entities;

    private EntityId _cachedEntity;

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
        /*  if (_cachedEntity.IsValid && _cachedEntity != entity)
              Entities.Core.GetSourceById(_cachedEntity).Resolver = RenderResolver.None;
          */
        var entityId = _cachedEntity = new EntityId(entity.Identifier);
        var writer = Entities.Core.GetEntityWriter(entityId);
        TransformStable.MakeFrom(in Transform.UnsafeAs(ref writer.Transform), out state.Transform);
        state.Bounds = writer.Box.Bounds;
        writer.Source.Resolver = RenderResolver.Highlight;
    }

    public void DeselectEntity(EditorId entity)
    {
        var entityId = new EntityId(entity.Identifier);
        var writer = Entities.Core.GetEntityWriter(entityId);
        writer.Source.Resolver = RenderResolver.None;
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
        var writer = Entities.Core.GetEntityWriter(entityId);
        writer.Box.Bounds = data.Bounds;
        writer.Transform.Translation = data.Transform.Translation;
        writer.Transform.Rotation = data.Transform.Rotation;
        writer.Transform.Scale = data.Transform.Scale;
    }

    public void FetchAnimation(EditorId entity, ref EditorAnimationState state)
    {
        var entityId = new EntityId(entity.Identifier);
        ref readonly var component = ref Entities.Animations.GetById(entityId);
        state.Animation = new EditorId(component.Animation, EditorItemType.Animation);
        state.ClipIndex = component.ClipIndex;
        state.Time = component.Time;
        state.Speed = component.Speed;
        state.Duration = component.Duration;
    }

    public void CommitAnimation(EditorId entity, in EditorAnimationState state)
    {
        var entityId = new EntityId(entity.Identifier);
        ref var component = ref Entities.Animations.GetById(entityId);
        component.ClipIndex = component.ClipIndex;
        component.Time = state.Time;
        component.Speed = state.Speed;
        component.Duration = state.Duration;
    }

    public void FetchParticle(EditorId entity, ref EditorParticleState state)
    {
        var entityId = new EntityId(entity.Identifier);
        var component = Entities.Particles.GetById(entityId);

        var emitter = apiContext.World.Particles.GetEmitter(component.EmitterHandle);
        state.EmitterHandle = emitter.EmitterHandle;
        state.Definition = emitter.Definition;
        state.EmitterState = emitter.State;
    }

    public void CommitParticle(EditorId entity, in EditorParticleState state)
    {
        var emitter = apiContext.World.Particles.GetEmitter(state.EmitterHandle);
        emitter.Definition = state.Definition;
        emitter.State = state.EmitterState;
    }
}