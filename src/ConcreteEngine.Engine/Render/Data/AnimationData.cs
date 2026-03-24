using System.Numerics;
using System.Runtime.CompilerServices;
using ConcreteEngine.Core.Engine.Assets;

namespace ConcreteEngine.Engine.Render.Data;

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

internal readonly struct AnimationClipChannel
{
    public readonly float[] PositionTimes;
    public readonly float[] RotationTimes;

    public readonly Vector3[] Positions;
    public readonly Quaternion[] Rotations;

    public readonly int MaxLength;

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