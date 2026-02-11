using System.Runtime.CompilerServices;
using ConcreteEngine.Core.Renderer;
using ConcreteEngine.Engine.Assets;

namespace ConcreteEngine.Engine.Worlds;

internal readonly struct AnimationEntry(
    SkeletonData skeleton,
    AnimationChannel[] clips)
{
    public readonly SkeletonData Skeleton = skeleton;
    public readonly AnimationChannel[] Clips = clips;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public ReadOnlySpan<AnimationChannel> GetClip(int clip)
    {
        var len = Skeleton.Length;
        var start = clip * len;
        if ((uint)start + (uint)len > (uint)Clips.Length) throw new IndexOutOfRangeException();
        return Clips.AsSpan(start, len);
    }
}

internal sealed class AnimationTable
{
    private static AnimationId MakeId() => new(++_idx);
    private static int _idx;

    private AnimationEntry[] _animations = [];

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public ReadOnlySpan<AnimationChannel> GetAnimationData(AnimationId id, int clip, out SkeletonData skeleton)
    {
        var index = id.Index();
        if ((uint)index >= (uint)_animations.Length) throw new IndexOutOfRangeException();
        ref readonly var it = ref _animations[index];
        skeleton = it.Skeleton;
        return it.GetClip(clip);
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

            var clips = new AnimationChannel[animation.Clips.Count * animation.BoneCount];
            var len = animation.Clips.Count;
            for (var c = 0; c < len; c++)
            {
                var animationClip = animation.Clips[c];
                var clip = clips.AsSpan(c * animation.BoneCount, animation.BoneCount);

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