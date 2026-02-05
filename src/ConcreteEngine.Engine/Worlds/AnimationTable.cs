using ConcreteEngine.Core.Renderer;
using ConcreteEngine.Engine.Assets;

namespace ConcreteEngine.Engine.Worlds;

internal readonly struct AnimationEntry(
    SkeletonData skeleton,
    AnimationChannel[][] clips)
{
    public readonly SkeletonData Skeleton = skeleton;
    public readonly AnimationChannel[][] Clips = clips;
}

internal sealed class AnimationTable
{
    private static AnimationId MakeId() => new(++_idx);
    private static int _idx;

    private AnimationEntry[] _animations = [];

    public ref readonly SkeletonData GetAnimationData(AnimationId id, int clip,
        out ReadOnlySpan<AnimationChannel> channels)
    {
        var index = id.Index();
        ref readonly var it = ref _animations[index];
        channels = it.Clips[clip];
        return ref it.Skeleton;
    }


    public ref readonly AnimationEntry GetAnimation(AnimationId id)
    {
        var index = id.Index();
        ArgumentOutOfRangeException.ThrowIfGreaterThanOrEqual((uint)index, (uint)_animations.Length, nameof(id));
        return ref _animations[index];
    }

    public void Setup(AssetSystem assets)
    {
        var span = assets.Store.GetAssetList<Model>().GetAssets();

        var count = 0;
        foreach (var model in span)
        {
            if (!model.Info.IsAnimated) continue;
            count++;
        }

        _animations = new AnimationEntry[count];

        foreach (var model in span)
        {
            if (!model.Info.IsAnimated) continue;

            var animation = model.Animation!;
            var animationId = MakeId();
            model.AttachAnimation(animationId);

            var clips = new AnimationChannel[animation.Clips.Count][];
            for (var c = 0; c < clips.Length; c++)
            {
                var animationClip = animation.Clips[c];
                var clip = clips[c] = new AnimationChannel[animation.BoneCount];

                for (int b = 0; b < animation.BoneCount; b++)
                {
                    if (b >= animationClip.Channels.Length)
                        clip[b] = new AnimationChannel();
                    else
                        clip[b] = animationClip.Channels[b];
                }
            }

            _animations[animationId.Index()] = new AnimationEntry(animation.SkeletonData, clips);
        }
    }
}