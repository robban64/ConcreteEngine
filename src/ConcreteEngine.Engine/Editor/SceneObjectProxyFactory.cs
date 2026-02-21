using ConcreteEngine.Core.Engine.Scene;
using ConcreteEngine.Editor.Controller;
using ConcreteEngine.Engine.ECS;
using ConcreteEngine.Engine.ECS.RenderComponent;
using ConcreteEngine.Engine.Scene;
using ConcreteEngine.Engine.Worlds;

namespace ConcreteEngine.Engine.Editor;

internal static class SceneObjectProxyFactory
{
    public static SceneStore SceneStore = null!;
    public static World World = null!;

    internal static SourceProperty CreateSourceProperty(RenderEntityId entity)
    {
        return new SourceProperty
        {
            Getter = (prop) =>
            {
                var comp = Ecs.Render.Core.GetSource(entity);
                prop.Mesh = comp.Mesh;
                prop.MaterialId = comp.Material;
            },
            Setter = (prop) => { }
        };
    }


    internal static SpatialProperty CreateSpatialProperty(SceneObjectId id)
    {
        return new SpatialProperty
        {
            Getter = (prop) =>
            {
                var sceneObject = SceneStore.Get(id);
                if (sceneObject == null!) throw new ArgumentException($"SceneObject not found: {id}");
                prop.Fill(in sceneObject.GetTransform(), in sceneObject.GetBounds());
            },
            Setter = (props) =>
            {
                var sceneObject = SceneStore.Get(id);
                if (sceneObject == null!) throw new ArgumentException($"SceneObject not found: {id}");
                props.Transform.FillTransform(out var transform);
                sceneObject.SetSpatial(in transform, in props.Bounds);
            }
        };
    }

    internal static ParticleProperty CreateParticleProperty(RenderEntityId entity)
    {
        return new ParticleProperty
        {
            Getter = (prop) =>
            {
                var comp = Ecs.Render.Stores<ParticleComponent>.Store.TryGet(entity);
                if (comp.IsNull) throw new ArgumentException($"Entity not found: {entity}");
                var emitter = World.Particles.GetEmitter(comp.Value.Emitter);
                prop.Fill(emitter.EmitterHandle, emitter.ParticleCount, emitter.Definition, emitter.State);
            },
            Setter = (prop) =>
            {
                var comp = Ecs.Render.Stores<ParticleComponent>.Store.TryGet(entity);
                if (comp.IsNull) throw new ArgumentException($"Entity not found: {entity}");
                var emitter = World.Particles.GetEmitter(comp.Value.Emitter);

                emitter.State = prop.State;
                emitter.Definition = prop.Definition;
            }
        };
    }

    internal static AnimationProperty CreateAnimationProperty(RenderEntityId entity)
    {
        var comp = Ecs.Render.Stores<RenderAnimationComponent>.Store.TryGet(entity);
        if (comp.IsNull)
            throw new ArgumentException($"Entity not found: {entity}");

        var animation = World.Animations.GetAnimation(comp.Value.Animation);
        return new AnimationProperty
        {
            Animation = comp.Value.Animation,
            ClipCount = animation.Clips.Length,
            Getter = (prop) =>
            {
                var comp = Ecs.Render.Stores<RenderAnimationComponent>.Store.TryGet(entity);
                if (comp.IsNull) throw new ArgumentException($"Entity not found: {entity}");
                prop.Clip = comp.Value.Clip;
                prop.Time = comp.Value.Time;
                prop.Speed = 0;
                prop.Duration = 0;
            },
            Setter = (prop) =>
            {
                var comp = Ecs.Render.Stores<RenderAnimationComponent>.Store.TryGet(entity);
                if (comp.IsNull) throw new ArgumentException($"Entity not found: {entity}");
                comp.Value.Clip = (short)prop.Clip;
                comp.Value.Time = prop.Time;
            }
        };
    }
}