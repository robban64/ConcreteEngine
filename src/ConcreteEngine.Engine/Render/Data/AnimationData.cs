using System.Numerics;
using System.Runtime.CompilerServices;
using ConcreteEngine.Core.Common;
using ConcreteEngine.Core.Common.Collections;
using ConcreteEngine.Core.Engine.Graphics;

namespace ConcreteEngine.Engine.Render.Data;

internal readonly ref struct SkinningContext(
    ReadOnlySpan<byte> parentIndices,
    ReadOnlySpan<Matrix4x4> bindPose,
    ReadOnlySpan<Matrix4x4> inverseBindPose,
    ReadOnlySpan<AnimationChannel> channels)
{
    public readonly ReadOnlySpan<byte> ParentIndices = parentIndices;
    public readonly ReadOnlySpan<Matrix4x4> BindPose = bindPose;
    public readonly ReadOnlySpan<Matrix4x4> InverseBindPose = inverseBindPose;
    public readonly ReadOnlySpan<AnimationChannel> Channels = channels;
}

internal readonly struct AnimationChannel(AnimationClip.Channel channels)
{
    public readonly float[] PositionTimes = channels.PositionTimes;
    public readonly float[] RotationTimes = channels.RotationTimes;

    public readonly Vector3[] Positions = channels.Positions;
    public readonly Quaternion[] Rotations = channels.Rotations;

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