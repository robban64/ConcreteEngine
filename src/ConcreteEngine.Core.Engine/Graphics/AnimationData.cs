using System.Numerics;
using System.Runtime.CompilerServices;
using ConcreteEngine.Core.Common;
using ConcreteEngine.Core.Common.Collections;

namespace ConcreteEngine.Core.Engine.Graphics;

internal readonly ref struct SkinningContext
{
    public readonly ReadOnlySpan<byte> ParentIndices;
    public readonly ReadOnlySpan<Matrix4x4> BindPose;
    public readonly ReadOnlySpan<Matrix4x4> InverseBindPose;
    public readonly ReadOnlySpan<AnimationChannel> Channels;

    public SkinningContext(
        ReadOnlySpan<byte> parentIndices,
        ReadOnlySpan<Matrix4x4> bindPose,
        ReadOnlySpan<Matrix4x4> inverseBindPose,
        ReadOnlySpan<AnimationChannel> channels)
    {
        if (parentIndices.Length != channels.Length || parentIndices.Length != bindPose.Length ||
            parentIndices.Length != inverseBindPose.Length)
            Throwers.InvalidOperation("Length mismatch");

        ParentIndices = parentIndices;
        BindPose = bindPose;
        InverseBindPose = inverseBindPose;
        Channels = channels;
    }
}

public readonly struct AnimationChannel
{
    public readonly float[] PositionTimes;
    public readonly float[] RotationTimes;

    public readonly Vector3[] Positions;
    public readonly Quaternion[] Rotations;

    public AnimationChannel()
    {
        PositionTimes = [];
        RotationTimes = [];
        Positions = [];
        Rotations = [];
    }
    public AnimationChannel(AnimationClip.Channel channels)
    {
        PositionTimes = channels.PositionTimes;
        RotationTimes = channels.RotationTimes;
        Positions = channels.Positions;
        Rotations = channels.Rotations;
    }

    public int MaxLength
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => int.Max(PositionTimes.Length, RotationTimes.Length);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public UnsafeSpan<float> GetPositionTimes() => new(PositionTimes);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public UnsafeSpan<float> GetRotationTimes() => new(RotationTimes);
}