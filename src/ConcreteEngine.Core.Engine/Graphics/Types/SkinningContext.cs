using System.Numerics;
using System.Runtime.CompilerServices;
using ConcreteEngine.Core.Common;

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