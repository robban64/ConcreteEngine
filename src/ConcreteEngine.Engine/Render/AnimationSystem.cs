using System.Numerics;
using System.Runtime.CompilerServices;
using ConcreteEngine.Core.Common;
using ConcreteEngine.Core.Engine.Assets;
using ConcreteEngine.Core.Engine.Graphics;
using ConcreteEngine.Engine.Render.Data;

namespace ConcreteEngine.Engine.Render;

internal sealed class AnimationRig
{
    public readonly byte[] ParentIndices;
    public readonly Matrix4x4[] BindPose;
    public readonly Matrix4x4[] InverseBindPose;
    public readonly AnimationChannel[] Channels;

    public readonly Id16<ModelAnimation> AnimationId;

    public AnimationRig(ModelAnimation source, AnimationChannel[] channels)
    {
        ArgumentNullException.ThrowIfNull(source);
        ArgumentNullException.ThrowIfNull(channels);
        ArgumentOutOfRangeException.ThrowIfZero(source.AnimationId.Value, nameof(source.AnimationId));

        AnimationId = source.AnimationId;

        ParentIndices = source.ParentIndices;
        BindPose = source.BindPose;
        InverseBindPose = source.InverseBindPose;
        Channels = channels;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public SkinningContext GetSkinningContext(int clip)
    {
        var len = ParentIndices.Length;
        var start = clip * len;
        if ((uint)start + (uint)len > (uint)Channels.Length)
            Throwers.BufferOverflow(nameof(AnimationChannel), start, start + len);

        var channels = Channels.AsSpan(start, len);
        return new SkinningContext(ParentIndices, BindPose, InverseBindPose, channels);
    }
}

internal sealed class AnimationSystem
{
    private AnimationRig[] _animations = [];

    public int Count { get; private set; }

    internal AnimationSystem()
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

    private static AnimationChannel[] CreateClipChannels(ModelAnimation animation)
    {
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
                    clip[b] = new AnimationChannel(animationClip.Channels[b]);
            }
        }

        return clips;
    }
}