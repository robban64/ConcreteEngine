using System.Numerics;
using ConcreteEngine.Core.Common.Numerics;
using ConcreteEngine.Core.Engine.Assets;
using ConcreteEngine.Core.Engine.ECS;
using ConcreteEngine.Core.Engine.ECS.GameComponent;
using ConcreteEngine.Core.Engine.Graphics;

namespace ConcreteEngine.Core.Engine.Scene;


public sealed class AnimationInstance : BlueprintInstance
{
    public readonly ModelBlueprint Blueprint;
    public ModelAnimation AssetAnimation { get; }

    public AnimationInstance(ModelBlueprint blueprint, ModelAnimation assetAnimation)
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

    public override SceneObjectBlueprint GetBlueprint() => Blueprint;
}
