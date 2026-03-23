using System.Numerics;
using System.Runtime.CompilerServices;
using ConcreteEngine.Core.Engine.Assets;
using ConcreteEngine.Core.Renderer;
using ConcreteEngine.Engine.Assets;

namespace ConcreteEngine.Engine.Render;


internal readonly struct SkeletonMatrices
{
    public readonly byte[] ParentIndices;
    public readonly Matrix4x4[] BindPose;
    public readonly Matrix4x4[] InverseBindPose;

    public int Length => ParentIndices.Length;

    public SkeletonMatrices(Skeleton skeleton)
    {
        ParentIndices = new byte[skeleton.ParentIndices.Length];
        for (int i = 0; i < ParentIndices.Length; i++)
        {
            var value = skeleton.ParentIndices[i];
            ParentIndices[i] = value == -1 ? (byte)0 : (byte)skeleton.ParentIndices[i];
        }
        BindPose = skeleton.BindPose;
        InverseBindPose = skeleton.InverseBindPose;
    }
}

internal readonly struct AnimationClipData
{
    public readonly float[] PositionTimes;
    public readonly float[] RotationTimes;

    public readonly Vector3[] Positions;
    public readonly Quaternion[] Rotations;

    public readonly int MaxLength;

    public AnimationClipData(AnimationChannel channels)
    {
        PositionTimes = channels.PositionTimes;
        RotationTimes = channels.RotationTimes;

        Positions = channels.Positions;
        Rotations = channels.Rotations;
        MaxLength = channels.MaxLength;
    }
}

internal readonly struct AnimationEntry
{
    public readonly SkeletonMatrices Skeleton;
    public readonly AnimationClipData[] Clips;

    public AnimationEntry(Skeleton skeleton, AnimationClipData[] clips)
    {
        Skeleton = new SkeletonMatrices(skeleton);
        Clips = clips;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public ReadOnlySpan<AnimationClipData> GetClip(int clip)
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
    public ReadOnlySpan<AnimationClipData> GetAnimationData(AnimationId id, int clip, out SkeletonMatrices skeleton)
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

    public void Setup(AssetStore assets)
    {
        var span = assets.GetAssetList<Model>().GetAssetSpan();

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

            var clips = new AnimationClipData[animation.Clips.Count * animation.BoneCount];
            var len = animation.Clips.Count;
            for (var c = 0; c < len; c++)
            {
                var animationClip = animation.Clips[c];
                var clip = clips.AsSpan(c * animation.BoneCount, animation.BoneCount);

                for (int b = 0; b < animation.BoneCount; b++)
                {
                    if (b >= animationClip.Channels.Length)
                        clip[b] = new AnimationClipData();
                    else
                        clip[b] = new AnimationClipData(animationClip.Channels[b]);
                }
            }

            _animations[animationId.Index()] = new AnimationEntry(animation.Skeleton, clips);
        }
    }
}