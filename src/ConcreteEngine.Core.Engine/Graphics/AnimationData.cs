using System.Numerics;
using System.Runtime.CompilerServices;
using ConcreteEngine.Core.Common;
using ConcreteEngine.Core.Common.Collections;
using ConcreteEngine.Core.Common.Memory;

namespace ConcreteEngine.Core.Engine.Graphics;

internal readonly ref struct SkinningContext
{
    public readonly NativeClip Tracks;
    public readonly ReadOnlySpan<byte> ParentIndices;
    public readonly ReadOnlySpan<Matrix4x4> BindPose;
    public readonly ReadOnlySpan<Matrix4x4> InverseBindPose;
    
    public int Length
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => InverseBindPose.Length;
    }

    public SkinningContext(
        ReadOnlySpan<byte> parentIndices,
        ReadOnlySpan<Matrix4x4> bindPose,
        ReadOnlySpan<Matrix4x4> inverseBindPose,
        NativeClip tracks)
    {
        if (parentIndices.Length != tracks.Length || parentIndices.Length != bindPose.Length ||
            parentIndices.Length != inverseBindPose.Length)
            Throwers.InvalidOperation("Length mismatch");
        
        if(tracks.IsNull) Throwers.NullPointer(nameof(tracks));

        ParentIndices = parentIndices;
        BindPose = bindPose;
        InverseBindPose = inverseBindPose;
        Tracks = tracks;
    }
}

internal readonly struct NativeClip
{
    public readonly NativeView<NativeBoneTrack> BoneTracks;

    internal NativeClip(NativeView<NativeBoneTrack> boneTracks)
    {
        if(boneTracks.IsNull) Throwers.NullPointer(nameof(boneTracks));
        BoneTracks = boneTracks;
    }
    public bool IsNull => BoneTracks.IsNull;
    public int Length => BoneTracks.Length;
}

internal readonly unsafe struct NativeBoneTrack
{
    private readonly float* _data;

    public readonly int PosCount;
    public readonly int RotCount;

    public NativeBoneTrack(float* data, int posCount, int rotCount)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(posCount);
        ArgumentOutOfRangeException.ThrowIfNegative(rotCount);
        
        if(data == null && (posCount > 0 || rotCount > 0))
            Throwers.InvalidArgument(nameof(data));
        
        _data = data;
        PosCount = posCount;
        RotCount = rotCount;
    }
    
    public bool IsNull => _data == null;
    
    public int MaxLength
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => int.Max(PosCount, RotCount);
    }
    public bool IsEmpty
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => (PosCount == 0 && RotCount == 0) || _data == null;
    }

    public NativeView<float> PositionTimes
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => new(_data, PosCount);
    }

    public NativeView<float> RotationTimes
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => new(_data + PosCount, RotCount);
    }

    public NativeView<Vector3> Positions
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => new((Vector3*)(_data + (PosCount + RotCount)), PosCount);
    }

    public NativeView<Quaternion> Rotations
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => new((Quaternion*)(_data + (PosCount + RotCount + (PosCount * 3))), RotCount);
    }

}
/*
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
}*/