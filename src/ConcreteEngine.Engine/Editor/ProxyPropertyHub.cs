using ConcreteEngine.Core.Engine;
using ConcreteEngine.Editor.Bridge;
using ConcreteEngine.Engine.ECS;
using ConcreteEngine.Engine.ECS.RenderComponent;
using ConcreteEngine.Engine.Scene;
using ConcreteEngine.Engine.Worlds;

namespace ConcreteEngine.Engine.Editor;

internal static class ProxyPropertyHub
{
    public static SceneStore SceneStore = null!;
    public static World World = null!;
    
    internal static ProxyPropertyEntry CreateSourceProperty(RenderEntityId entity)
    {
        return new ProxyPropertyEntry<SourcePropertyValue>
        {
            Name = "Source Settings",
            Kind = ProxyPropertyKind.Source,
            GetValue = () =>
            {
                var comp = Ecs.Render.Core.GetSource(entity);
                return new SourcePropertyValue(comp.Model, comp.MaterialKey);
            },
            SetValue = (_) => false
        };
    }

    internal static ProxyPropertyEntry CreateSpatialProperty(SceneObjectId id)
    {
        return new ProxyPropertyEntry<SpatialPropertyValue>
        {
            Name = "Spatial Settings",
            Kind = ProxyPropertyKind.Spatial,
            GetValue = () =>
            {
                var sceneObject = SceneStore.Get(id);
                return new SpatialPropertyValue(sceneObject.GetTransform(), sceneObject.GetBounds());
            },
            SetValue = (data) =>
            {
                SceneStore.Get(id).SetSpatial(in data.Transform, in data.Bounds);
                return true;
            }
        };
    }

    internal static ProxyPropertyEntry CreateParticleProperty(RenderEntityId entity)
    {
        return new ProxyPropertyEntry<ParticlePropertyValue>
        {
            Name = "Emitter Settings",
            Kind = ProxyPropertyKind.Particle,
            GetValue = () =>
            {
                var comp = Ecs.Render.Stores<ParticleComponent>.Store.TryGet(entity);
                if (comp.IsNull) return default;
                
                var e = World.Particles.GetEmitter(comp.Value.Emitter);
                return new ParticlePropertyValue(e.EmitterHandle, e.ParticleCount, in e.Definition, in e.State);
            },
            SetValue = (data) =>
            {
                var comp = Ecs.Render.Stores<ParticleComponent>.Store.TryGet(entity);
                if (comp.IsNull) return false;
                
                var emitter = World.Particles.GetEmitter(comp.Value.Emitter);
                emitter.State = data.EmitterState;
                emitter.Definition = data.Definition;
                return true;
            }
        };
    }

    internal static ProxyPropertyEntry CreateAnimationProperty(RenderEntityId entity)
    {
        return new ProxyPropertyEntry<AnimationPropertyValue>
        {
            Name = "Animation Settings",
            Kind = ProxyPropertyKind.Animation,
            GetValue = () =>
            {
                var comp = Ecs.Render.Stores<RenderAnimationComponent>.Store.TryGet(entity);
                if (comp.IsNull) return default;
                
                ref readonly var it = ref comp.Value;
                return new AnimationPropertyValue(it.Animation, it.Clip, 4)
                {
                    Time = it.Time, Speed = it.Speed, Duration = it.Duration
                };
            },
            SetValue = (data) =>
            {
                var comp = Ecs.Render.Stores<RenderAnimationComponent>.Store.TryGet(entity);
                if (comp.IsNull) return false;
                
                ref var it = ref comp.Value;
                it.Clip = (short)data.Clip;
                it.Time = data.Time;
                it.Speed = data.Speed;
                it.Duration = data.Duration;
                return true;
            }
        };
    }


    public static SpatialPropertyValue GetSpatial(SceneStore store, SceneObjectId id)
    {
        var sceneObject = store.Get(id);
        return new SpatialPropertyValue { Transform = sceneObject.GetTransform(), Bounds = sceneObject.GetBounds() };
    }

    public static bool SetSpatial(SceneStore store, SceneObjectId id, SpatialPropertyValue value)
    {
        store.Get(id).SetSpatial(in value.Transform, in value.Bounds);
        return true;
    }
}