using System.Numerics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using ConcreteEngine.Core.Common;
using ConcreteEngine.Core.Common.Memory;

namespace ConcreteEngine.Core.Engine.Graphics.Animations;

[StructLayout(LayoutKind.Sequential)]
internal readonly unsafe struct NativeClip
{
    public readonly int Length;
    public readonly NativeBoneTrack* BoneTracks;

    internal NativeClip(NativeView<NativeBoneTrack> boneTracks)
    {
        if (boneTracks.IsNull) Throwers.NullPointer(nameof(boneTracks));
        BoneTracks = boneTracks;
        Length = boneTracks.Length;
    }
    
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public NativeView<NativeBoneTrack> AsView() => new (BoneTracks, Length);
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public ReadOnlySpan<NativeBoneTrack> AsSpan() => new (BoneTracks, Length);

    public bool IsNull => BoneTracks == null;

}

[StructLayout(LayoutKind.Sequential)]
internal readonly unsafe struct NativeBoneTrack
{
    public readonly int PosCount;
    public readonly int RotCount;
    public readonly int PositionIndex;
    public readonly int RotationIndex;

    private readonly float* _data;

    public NativeBoneTrack(float* data, int posCount, int rotCount)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(posCount);
        ArgumentOutOfRangeException.ThrowIfNegative(rotCount);

        if (data == null && (posCount > 0 || rotCount > 0))
            Throwers.InvalidArgument(nameof(data));

        _data = data;
        PosCount = posCount;
        RotCount = rotCount;
        PositionIndex = posCount + rotCount;
        RotationIndex = posCount + rotCount + (posCount * 3);
    }

    public bool IsNull => _data == null;

    public bool IsEmpty
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => (PosCount == 0 && RotCount == 0) || _data == null;
    }
    public float* PositionTimesPtr => _data;
    
    public float* RotationTimesPtr
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => _data + PosCount;
    }
    
    public Vector3* PositionPtr
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => (Vector3*)(_data + PositionIndex);
    }

    public Quaternion* RotationPtr
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => (Quaternion*)(_data + RotationIndex);
    }
    

    public NativeView<float> PositionTimes
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => new(_data, PosCount);
    }

    public NativeView<float> RotationTimes
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => new(RotationTimesPtr, RotCount);
    }

    public NativeView<Vector3> Positions
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => new(PositionPtr, PosCount);
    }

    public NativeView<Quaternion> Rotations
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => new(RotationPtr, RotCount);
    }
}