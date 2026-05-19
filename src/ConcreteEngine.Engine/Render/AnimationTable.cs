using System.Numerics;
using System.Runtime.CompilerServices;
using ConcreteEngine.Core.Common;
using ConcreteEngine.Core.Engine.Assets;
using ConcreteEngine.Core.Engine.Graphics;
using ConcreteEngine.Engine.Render.Data;
using ConcreteEngine.Renderer.Core;

namespace ConcreteEngine.Engine.Render;

internal sealed class AnimationRig
{
    public readonly Id16<ModelAnimation> AnimationId;
    public readonly byte[] ParentIndices;
    public readonly Matrix4x4[] BindPose;
    public readonly Matrix4x4[] InverseBindPose;
    public readonly AnimationClipChannel[] Channels;
    
    public AnimationRig(ModelAnimation source, AnimationClipChannel[] channels)
    {
        ArgumentNullException.ThrowIfNull(source);
        ArgumentNullException.ThrowIfNull(channels);
        ArgumentOutOfRangeException.ThrowIfZero(source.AnimationId.Value, nameof(source.AnimationId));
        ArgumentOutOfRangeException.ThrowIfZero(channels.Length, nameof(channels));
        
        AnimationId = source.AnimationId;
        Channels = channels;

        ParentIndices = source.Skeleton.ParentIndices;
        BindPose = source.Skeleton.BindPose;
        InverseBindPose = source.Skeleton.InverseBindPose;
    }
    
    public SkinningContext GetSkinningContext(int clip)
    {
        var len = Channels.Length;
        var start = clip * len;
        if ((uint)start + (uint)len > (uint)Channels.Length)
            Throwers.BufferOverflow(nameof(AnimationClipChannel), Channels.Length, start + len);
        
        var channels = Channels.AsSpan(start, len);
        return new SkinningContext(ParentIndices, BindPose, InverseBindPose, channels);
    }

    public ReadOnlySpan<AnimationClipChannel> GetClip(int clip)
    {
        var len = Channels.Length;
        var start = clip * len;
        if ((uint)start + (uint)len > (uint)Channels.Length)
            Throwers.BufferOverflow(nameof(AnimationClipChannel), Channels.Length, start + len);

        return Channels.AsSpan(start, len);
    }
}


internal sealed class AnimationTable
{
    public static AnimationTable Instance { get; private set; } = null!;
    public static AnimationTable Make() => Instance = new AnimationTable();

    private ModelAnimation[] _modelAnimations = [];
    private AnimationRig[] _animations = [];
    
    public int Count { get; private set; }

    private AnimationTable()
    {
        if (Instance is not null) throw new InvalidOperationException("AnimationTable already created");
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public SkinningContext GetSkinningContext(Id16<ModelAnimation> id, int clip)
    {
        var index = id.Index();
        ArgumentOutOfRangeException.ThrowIfGreaterThanOrEqual((uint)index, (uint)_animations.Length, nameof(id));
        return _animations[index].GetSkinningContext(clip);
    }



    public void Setup(AssetStore assets)
    {
        var count = 0;
        foreach (var model in assets.GetAssetEnumerator<Model>())
        {
            if (model.Animation is null) continue;
            count++;
        }

        _animations = new AnimationRig[count];
        _modelAnimations = new ModelAnimation[count];

        var index = 0;
        foreach (var model in assets.GetAssetEnumerator<Model>())
        {
            if (model.Animation is null) continue;
            
            var animation = model.Animation!;
            var animationId = new Id16<ModelAnimation>(index + 1);
            
            _modelAnimations[index] = animation;
            animation.AnimationId = animationId;
            
            var clips = CreateClipChannels(animation);
            _animations[index] = new AnimationRig(animation, clips);
            index++;
        }
    }

    private static AnimationClipChannel[] CreateClipChannels(ModelAnimation animation)
    {
        var clips = new AnimationClipChannel[animation.Clips.Count * animation.BoneCount];
        var len = animation.Clips.Count;
        for (var c = 0; c < len; c++)
        {
            var animationClip = animation.Clips[c];
            var clip = clips.AsSpan(c * animation.BoneCount, animation.BoneCount);

            for (int b = 0; b < animation.BoneCount; b++)
            {
                if (b >= animationClip.Channels.Length)
                    clip[b] = new AnimationClipChannel();
                else
                    clip[b] = new AnimationClipChannel(animationClip.Channels[b]);
            }
        }
        return clips;
    }
}