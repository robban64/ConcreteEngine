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
    public readonly ReadOnlySpan<BoneTrack> Channels;

    public SkinningContext(
        ReadOnlySpan<byte> parentIndices,
        ReadOnlySpan<Matrix4x4> bindPose,
        ReadOnlySpan<Matrix4x4> inverseBindPose,
        ReadOnlySpan<BoneTrack> channels)
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

public readonly struct BoneTrack
{
    public readonly float[] PositionTimes;
    public readonly float[] RotationTimes;

    public readonly Vector3[] Positions;
    public readonly Quaternion[] Rotations;

    public BoneTrack()
    {
        PositionTimes = [];
        RotationTimes = [];
        Positions = [];
        Rotations = [];
    }
    
    public BoneTrack(int positionLength, int rotationLength)
    {
        PositionTimes = new float[positionLength];
        RotationTimes = new float[rotationLength];

        Positions = new Vector3[positionLength];
        Rotations = new Quaternion[rotationLength];
    }

    

    public bool IsNull => PositionTimes == null || RotationTimes == null || Positions ==  null || Rotations == null;

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