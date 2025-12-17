using ConcreteEngine.Engine.Assets;
using ConcreteEngine.Engine.Assets.Materials;
using ConcreteEngine.Engine.Worlds;
using ConcreteEngine.Engine.Worlds.Entities;
using ConcreteEngine.Engine.Worlds.Entities.Components;
using ConcreteEngine.Engine.Worlds.Utility;

namespace ConcreteEngine.Engine.Scene;

internal readonly ref struct EntityFactoryContext(
    in WorldContext world,
    AssetStore assetStore,
    MaterialStore materialStore)
{
    public readonly WorldContext World = world;
    public readonly AssetStore AssetStore = assetStore;
    public readonly MaterialStore MaterialStore = materialStore;
}

internal delegate void ComponentFactoryDel<in TData>(EntityId entity, TData data, in WorldContext ctx)
    where TData : class, IComponentTemplate;

internal static class GameEntityFactory
{
    private static readonly Dictionary<Type, Delegate> Factories = new(4);

    public static void Initialize()
    {
        Factories[typeof(SpatialComponentTemplate)] = BuildSpatial;
        Factories[typeof(ModelComponentTemplate)] = BuildModel;
        Factories[typeof(AnimationComponentTemplate)] = BuildAnimation;
        Factories[typeof(ParticleComponentTemplate)] = BuildParticle;
    }

    public static void Invoke<TData>(EntityId entity, TData data, in WorldContext ctx)
        where TData : class, IComponentTemplate
    {
        ArgumentNullException.ThrowIfNull(data);
        ((ComponentFactoryDel<TData>)Factories[typeof(TData)])(entity, data, in ctx);
    }
    /*
    public static void InvokeAll<TData>(ReadOnlySpan<TData> span, in GameEntityFactoryContext ctx) where TData : class, IComponentTemplate
    {
        ArgumentOutOfRangeException.ThrowIfZero(span.Length);
        var del = ((ComponentFactoryDel<TData>)Factories[typeof(TData)]);
        foreach (var data in span) del(data, in ctx);
    }*/

    private static void BuildSpatial(EntityId entity, SpatialComponentTemplate data, in WorldContext ctx)
    {
        var view = ctx.Entities.Core.GetEntityView(entity);
        view.Transform = data.Transform;
        view.Box = data.Bounds;
    }

    private static void BuildModel(EntityId entity, ModelComponentTemplate data, in WorldContext ctx)
    {
        var materialTag = MaterialTagBuilder.FromSpan(data.Materials);
        var materialKey = ctx.MaterialTable.Add(in materialTag);
        ref var source = ref ctx.Entities.Core.GetSource(entity);
        source.Model = data.Model;
        source.MaterialKey = materialKey;

    }

    private static void BuildAnimation(EntityId entity, AnimationComponentTemplate data, in WorldContext ctx)
    {
        var component = new AnimationComponent
        {
            Animation = data.Animation,
            Clip = data.Clip,
            Duration = data.Duration,
            Speed = data.Speed,
            Time = data.Time
        };
        ctx.Entities.AddComponent(entity, component);
    }

    private static void BuildParticle(EntityId entity, ParticleComponentTemplate data, in WorldContext ctx)
    {
        var view = ctx.Entities.Core.GetEntityView(entity);

    }
}