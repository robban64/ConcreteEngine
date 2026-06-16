using System.Numerics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace ConcreteEngine.Core.Engine.Graphics;

internal readonly ref struct SkinningContext
{
    public readonly int Length;

    public readonly ref readonly NativeClip Tracks;
    private readonly ref byte _parentIndices;
    private readonly ref Matrix4x4 _bindPose;
    private readonly ref Matrix4x4 _inverseBindPose;

    public SkinningContext(
        ReadOnlySpan<byte> parentIndices,
        ReadOnlySpan<Matrix4x4> bindPose,
        ReadOnlySpan<Matrix4x4> inverseBindPose,
        ref NativeClip tracks)
    {
        Length = parentIndices.Length;
        _parentIndices = ref MemoryMarshal.GetReference(parentIndices);
        _bindPose = ref MemoryMarshal.GetReference(bindPose);
        _inverseBindPose = ref MemoryMarshal.GetReference(inverseBindPose);
        Tracks = ref tracks;
    }


    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public ref readonly byte GetParentIndices(int i) => ref Unsafe.Add(ref _parentIndices, i);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public ref readonly Matrix4x4 GetBindPose(int i) => ref Unsafe.Add(ref _bindPose, i);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public ref readonly Matrix4x4 GetInverseBindPose(int i) => ref Unsafe.Add(ref _inverseBindPose, i);
}