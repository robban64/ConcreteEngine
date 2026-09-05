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

    public bool IsNull => BoneTracks == null;

}

[StructLayout(LayoutKind.Sequential)]
internal readonly unsafe struct NativeBoneTrack
{
    public readonly int PosCount;
    public readonly int RotCount;

    private readonly float* _data;
    private readonly Vector3* _positions;
    private readonly Quaternion* _rotations;

    public NativeBoneTrack(float* data, int posCount, int rotCount)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(posCount);
        ArgumentOutOfRangeException.ThrowIfNegative(rotCount);

        if (data == null && (posCount > 0 || rotCount > 0))
            Throwers.InvalidArgument(nameof(data));

        PosCount = posCount;
        RotCount = rotCount;

        _data = data;
        _positions = (Vector3*)(data + posCount + rotCount);
        _rotations = (Quaternion*)(data + posCount + rotCount + (posCount * 3));

    }

    public bool IsNull => _data == null;

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
        get => new(_positions, PosCount);
    }

    public NativeView<Quaternion> Rotations
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => new(_rotations, RotCount);
    }
}