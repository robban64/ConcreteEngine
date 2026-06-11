using System.Numerics;
using System.Runtime.CompilerServices;
using ConcreteEngine.Core.Common;
using ConcreteEngine.Core.Engine.Assets;
using ConcreteEngine.Core.Engine.ECS;
using ConcreteEngine.Core.Engine.ECS.GameComponent;

namespace ConcreteEngine.Core.Engine.Graphics;

internal sealed class AnimationManager
{
    private ModelRig[] _animations = [];

    public int Count { get; private set; }

    internal AnimationManager()
    {
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public SkinningContext GetSkinningContext(Id16<ModelRig> id, int clip)
    {
        var index = id.Index();
        if ((uint)index >= (uint)_animations.Length)
            Throwers.IndexOutOfRange(nameof(ModelRig), index, _animations.Length);
        return _animations[index].GetSkinningContext(clip);
    }
    
    public void Simulate(float dt)
    {
        foreach (var query in Ecs.Game.Query<AnimationComponent>())
        {
            ref var c = ref query.Component;
            c.AdvanceTime(dt);
        }
    }

    public void Setup(AssetStore assets)
    {
        int count = 0;
        foreach (var model in assets.GetAssetEnumerator<Model>())
        {
            if (model.Animation is null) continue;
            count++;
        }

        Count = count;
        _animations = new ModelRig[count];
        foreach (var model in assets.GetAssetEnumerator<Model>())
        {
            if (model.Animation is not {} rig) continue;
            _animations[rig.Id.Index()] = rig;
        }
    }

}