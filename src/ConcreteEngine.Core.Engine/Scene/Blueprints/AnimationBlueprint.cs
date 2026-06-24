namespace ConcreteEngine.Core.Engine.Scene;

/*
public sealed class AnimationInstance : BlueprintInstance
{
    public override SceneObjectBlueprint GetBlueprint() => Blueprint;

    public readonly ModelBlueprint Blueprint;
    public ModelAnimation AssetAnimation { get; }

    public AnimationInstance(SceneObject owner, ModelBlueprint blueprint, ModelAnimation assetAnimation) : base(owner)
    {
        Blueprint = blueprint;
        AssetAnimation = assetAnimation;
    }

    public ref AnimationComponent GetComponent()
    {
        var store = Ecs.Game.Stores<AnimationComponent>.Store;
        foreach (var entity in GameEntityIds)
        {
            if (store.Has(entity)) return ref store.Get(entity);
        }

        throw new InvalidOperationException();
    }

    internal override void OnCreate()
    {
        var clip = AssetAnimation.Clips[0];
        var modelInstance = Owner.GetInstance<ModelInstance>();

        var renderComponent = new SkinningComponent(AssetAnimation.AnimationId, instance: 0);
        var gameComponent = new AnimationComponent { Duration = clip.Duration, Speed = clip.TicksPerSecond };

        var existing = false;
        var rootEntity = modelInstance.GetRenderEntities()[0];
        foreach (var query in Ecs.GetRenderStore<SkinningComponent>().Query())
        {
            ref readonly var c = ref query.Component;
            if (renderComponent.AnimationId != c.AnimationId || renderComponent.Instance != c.Instance)
                continue;

            existing = true;
            rootEntity = query.Entity;
            break;
        }

        if (!existing)
        {
            Ecs.GetRenderStore<SkinningComponent>().Add(rootEntity, renderComponent);

            var gameEntity = Ecs.GameCore.AddEntity();
            GameEntityIds.Add(gameEntity);
            Ecs.GetGameStore<AnimationComponent>().Add(gameEntity, gameComponent);
            Ecs.GetGameStore<RenderLink>().Add(gameEntity, new RenderLink(rootEntity));
        }

        var skinLinkComponent = new SkinLinkComponent { EntityId = rootEntity };
        foreach (var entity in GetRenderEntities())
        {
            Ecs.GetRenderStore<SkinLinkComponent>().Add(entity, skinLinkComponent);
        }
    }
}
*/