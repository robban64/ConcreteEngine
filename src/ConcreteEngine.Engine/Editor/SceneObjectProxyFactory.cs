using ConcreteEngine.Core.Engine.Scene;
using ConcreteEngine.Editor.Bridge;
using ConcreteEngine.Engine.ECS;
using ConcreteEngine.Engine.ECS.RenderComponent;
using ConcreteEngine.Engine.Scene;
using ConcreteEngine.Engine.Worlds;

namespace ConcreteEngine.Engine.Editor;

internal static class SceneObjectProxyFactory
{
    public static SceneStore SceneStore = null!;
    public static World World = null!;

    internal static ProxyPropertyEntry CreateSourceProperty(RenderEntityId entity)
    {
        return new ProxyPropertyEntry<SourceProperty>
        {
            Name = "Source Settings",
            Kind = ProxyPropertyKind.Source,
            InvokeFetch = (out property) =>
            {
                var comp = Ecs.Render.Core.GetSource(entity);
                property = new SourceProperty(comp.Mesh, comp.Material);
            },
            InvokeSet = (in _) => false
        };
    }

    internal static ProxyPropertyEntry CreateSpatialProperty(SceneObjectId id)
    {
        return new ProxyPropertyEntry<SpatialProperty>
        {
            Name = "Spatial Settings",
            Kind = ProxyPropertyKind.Spatial,
            InvokeFetch = (out property) =>
            {
                var sceneObject = SceneStore.Get(id);
                property = new SpatialProperty(sceneObject.GetTransform(), sceneObject.GetBounds());
            },
            InvokeSet = (in data) =>
            {
                SceneStore.Get(id).SetSpatial(in data.Transform, in data.Bounds);
                return true;
            }
        };
    }

    internal static ProxyPropertyEntry CreateParticleProperty(RenderEntityId entity)
    {
        return new ProxyPropertyEntry<ParticleProperty>
        {
            Name = "Emitter Settings",
            Kind = ProxyPropertyKind.Particle,
            InvokeFetch = (out property) =>
            {
                var comp = Ecs.Render.Stores<ParticleComponent>.Store.TryGet(entity);
                if (comp.IsNull)
                {
                    property = default;
                    return;
                }

                var e = World.Particles.GetEmitter(comp.Value.Emitter);
                property = new ParticleProperty(e.EmitterHandle, e.ParticleCount, in e.Definition, in e.State);
            },
            InvokeSet = (in data) =>
            {
                var comp = Ecs.Render.Stores<ParticleComponent>.Store.TryGet(entity);
                if (comp.IsNull) return false;

                var emitter = World.Particles.GetEmitter(comp.Value.Emitter);
                emitter.State = data.State;
                emitter.Definition = data.Definition;
                return true;
            }
        };
    }

    internal static ProxyPropertyEntry CreateAnimationProperty(RenderEntityId entity)
    {
        return new ProxyPropertyEntry<AnimationProperty>
        {
            Name = "Animation Settings",
            Kind = ProxyPropertyKind.Animation,
            InvokeFetch = (out property) =>
            {
                var comp = Ecs.Render.Stores<RenderAnimationComponent>.Store.TryGet(entity);
                if (comp.IsNull)
                {
                    property = default;
                    return;
                }

                ref readonly var it = ref comp.Value;
                property = new AnimationProperty(it.Animation, it.Clip, 4)
                {
                    Time = it.Time, Speed = 0, Duration = 0
                };
            },
            InvokeSet = (in data) =>
            {
                var comp = Ecs.Render.Stores<RenderAnimationComponent>.Store.TryGet(entity);
                if (comp.IsNull) return false;

                ref var it = ref comp.Value;
                it.Clip = (short)data.Clip;
                it.Time = data.Time;
                //it.Speed = data.Speed;
                //it.Duration = data.Duration;
                return true;
            }
        };
    }


    public static SpatialProperty GetSpatial(SceneStore store, SceneObjectId id)
    {
        var sceneObject = store.Get(id);
        return new SpatialProperty { Transform = sceneObject.GetTransform(), Bounds = sceneObject.GetBounds() };
    }

    public static bool SetSpatial(SceneStore store, SceneObjectId id, SpatialProperty value)
    {
        store.Get(id).SetSpatial(in value.Transform, in value.Bounds);
        return true;
    }
}