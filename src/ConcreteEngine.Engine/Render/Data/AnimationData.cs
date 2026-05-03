using System.Numerics;
using System.Runtime.CompilerServices;
using ConcreteEngine.Core.Common.Memory;
using ConcreteEngine.Core.Engine.Assets;
using ConcreteEngine.Core.Engine.Graphics;

namespace ConcreteEngine.Engine.Render.Data;

internal readonly struct SkeletonMatrices
{
    public readonly byte[] ParentIndices;
    public readonly Matrix4x4[] BindPose;
    public readonly Matrix4x4[] InverseBindPose;

    public int Length => ParentIndices.Length;

    public SkeletonMatrices(Skeleton s)
    {
        ArgumentOutOfRangeException.ThrowIfNotEqual(s.BindPose.Length, s.InverseBindPose.Length, nameof(s));
        ArgumentOutOfRangeException.ThrowIfNotEqual(s.BindPose.Length, s.ParentIndices.Length, nameof(s));

        ParentIndices = new byte[s.ParentIndices.Length];
        for (int i = 0; i < ParentIndices.Length; i++)
        {
            var value = s.ParentIndices[i];
            ParentIndices[i] = value == -1 ? (byte)0 : (byte)s.ParentIndices[i];
        }

        BindPose = s.BindPose;
        InverseBindPose = s.InverseBindPose;
    }
}

internal readonly struct AnimationClipChannel
{
    public readonly float[] PositionTimes;
    public readonly float[] RotationTimes;

    public readonly Vector3[] Positions;
    public readonly Quaternion[] Rotations;

    public readonly int MaxLength;
    
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public UnsafeSpan<float> GetPositionTimes() => new(PositionTimes);
    
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public UnsafeSpan<float> GetRotationTimes() => new(RotationTimes);

    public AnimationClipChannel(AnimationChannel channels)
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
    public readonly AnimationClipChannel[] Clips;

    public AnimationEntry(Skeleton skeleton, AnimationClipChannel[] clips)
    {
        Skeleton = new SkeletonMatrices(skeleton);
        Clips = clips;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public ReadOnlySpan<AnimationClipChannel> GetClip(int clip)
    {
        var len = Skeleton.Length;
        var start = clip * len;
        if ((uint)start + (uint)len > (uint)Clips.Length) throw new IndexOutOfRangeException();
        return Clips.AsSpan(start, len);
    }
}