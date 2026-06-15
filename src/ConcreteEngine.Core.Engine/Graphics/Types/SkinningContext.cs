using System.Numerics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using ConcreteEngine.Core.Common;
using ConcreteEngine.Core.Common.Memory;

namespace ConcreteEngine.Core.Engine.Graphics;

internal readonly ref struct SkinningContext
{
    public readonly int Length;

    private readonly ref NativeBoneTrack _tracks;
    private readonly ref byte _parentIndices;
    private readonly ref Matrix4x4 _bindPose;
    private readonly ref Matrix4x4 _inverseBindPose;


    public SkinningContext(
        byte[] parentIndices,
        Matrix4x4[] bindPose,
        Matrix4x4[] inverseBindPose,
        NativeClip tracks)
    {
        if (parentIndices.Length != tracks.Length || parentIndices.Length != bindPose.Length ||
            parentIndices.Length != inverseBindPose.Length)
        {
            Throwers.InvalidOperation("Length mismatch");
        }

        if (tracks.IsNull) Throwers.NullPointer(nameof(tracks));

        Length = parentIndices.Length;
        _parentIndices = ref MemoryMarshal.GetArrayDataReference(parentIndices);
        _bindPose = ref MemoryMarshal.GetArrayDataReference(bindPose);
        _inverseBindPose = ref MemoryMarshal.GetArrayDataReference(inverseBindPose);
        _tracks = ref tracks.BoneTracks[0];
    }


    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public ref readonly NativeBoneTrack GetBoneTrack(int i) => ref Unsafe.Add(ref _tracks, i);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public byte GetParentIndices(int i) => Unsafe.Add(ref _parentIndices, i);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public ref readonly Matrix4x4 GetBindPose(int i) => ref Unsafe.Add(ref _bindPose, i);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public ref readonly Matrix4x4 GetInverseBindPose(int i) => ref Unsafe.Add(ref _inverseBindPose, i);
}