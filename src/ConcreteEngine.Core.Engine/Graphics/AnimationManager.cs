using System.Numerics;
using System.Runtime.CompilerServices;
using ConcreteEngine.Core.Common;
using ConcreteEngine.Core.Engine.Assets;
using ConcreteEngine.Core.Engine.ECS;
using ConcreteEngine.Core.Engine.ECS.GameComponent;

namespace ConcreteEngine.Core.Engine.Graphics;

internal sealed class AnimationManager
{
    private AnimationRig[] _animations = [];

    public int Count { get; private set; }

    internal AnimationManager()
    {
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public SkinningContext GetSkinningContext(Id16<ModelAnimation> id, int clip)
    {
        var index = id.Index();
        if ((uint)index >= (uint)_animations.Length)
            Throwers.BufferOverflow(nameof(AnimationChannel), index, _animations.Length);
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
        int count = 0, idHeigh = 0;
        foreach (var model in assets.GetAssetEnumerator<Model>())
        {
            if (model.Animation is null) continue;
            count++;
            idHeigh = int.Max(idHeigh, model.Animation.AnimationId.Value);
        }

        var length = Count = int.Max(idHeigh, count);
        _animations = new AnimationRig[length];

        foreach (var model in assets.GetAssetEnumerator<Model>())
        {
            if (model.Animation is null) continue;
            var animation = model.Animation!;
            var index = animation.AnimationId.Index();
            var clips = CreateClipChannels(animation);

            _animations[index] = new AnimationRig(animation, clips);
        }
    }

    private static AnimationChannel[][] CreateClipChannels(ModelAnimation animation)
    {
        var result = new AnimationChannel[animation.Clips.Length][];
        var len = animation.Clips.Length;
        for (var c = 0; c < len; c++)
        {
            var clip = animation.Clips[c];
            result[c] = new AnimationChannel[clip.Length];
            for (int b = 0; b < clip.Length; b++)
            {
                result[c][b] = new AnimationChannel(clip.Channels[b]);
            }
        }

        return result;
    }
}