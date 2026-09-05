using System.Numerics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using ConcreteEngine.Core.Common;
using ConcreteEngine.Core.Common.Memory;
using ConcreteEngine.Core.Engine.Graphics.Animations;

namespace ConcreteEngine.Core.Engine.Graphics;

public sealed class ModelRig : IDisposable
{
    public readonly int ClipCount;
    public readonly int BoneCount;

    public readonly Dictionary<string, int> BoneMapping;

    private readonly AnimationClip[] _clips;

    private readonly byte[] _parentIndices;
    private readonly Matrix4x4[] _bindPose;
    private readonly Matrix4x4[] _inverseBindPose;

    private NativeArray<byte> _clipsBuffer;
    private NativeView<NativeClip> _clipsView;


    internal ModelRig(
        Dictionary<string, int> boneMapping,
        ReadOnlySpan<byte> parentIndices,
        ReadOnlySpan<Matrix4x4> bindPose,
        ReadOnlySpan<Matrix4x4> inverseBindPose,
        ReadOnlySpan<AnimationClip> clips,
        NativeArray<byte> clipsBuffer)
    {
        ArgumentOutOfRangeException.ThrowIfZero(parentIndices.Length);
        ArgumentOutOfRangeException.ThrowIfZero(clipsBuffer.Length);
        ArgumentOutOfRangeException.ThrowIfZero(clips.Length);

        if (clipsBuffer.IsNull) Throwers.NullPointer(nameof(clipsBuffer));

        if (boneMapping.Count != parentIndices.Length && parentIndices.Length != bindPose.Length ||
            parentIndices.Length != inverseBindPose.Length)
        {
            Throwers.InvalidArgument("Length mismatch");
        }

        foreach (var clip in clips)
        {
            if (clip == null!) Throwers.InvalidArgument(nameof(clips));
        }

        ClipCount = clips.Length;
        BoneCount = parentIndices.Length;

        BoneMapping = boneMapping;
        _clips = clips.ToArray();
        _parentIndices = parentIndices.ToArray();
        _bindPose = bindPose.ToArray();
        _inverseBindPose = inverseBindPose.ToArray();
        _clipsBuffer = clipsBuffer;
        _clipsView = GetValidateClipBuffer(_clipsBuffer, ClipCount);
    }

    public AnimationClip GetClip(int clip) => _clips[clip];

    public ReadOnlySpan<byte> ParentIndices(int count) => new(_parentIndices, 0, count);
    public ReadOnlySpan<Matrix4x4> BindPose(int count) => new(_bindPose, 0, count);
    public ReadOnlySpan<Matrix4x4> InverseBindPose(int count) => new(_inverseBindPose, 0, count);
    public Matrix4x4[] BindPoseArray => _bindPose;
    public Matrix4x4[] InverseBindPoseArray => _inverseBindPose;
    public byte[] ParentIndicesArray => _parentIndices;

    internal NativeClip GetClipTracks(int clip)
    {
        if ((uint)clip > (uint)ClipCount || _clipsView.IsNull)
            Throwers.InvalidOperation(nameof(_clipsBuffer));

        var it = _clipsView[clip];
        if (it.IsNull || it.Length == 0) 
            Throwers.InvalidOperation(nameof(_clipsBuffer));

        return it;
    }

    public void Dispose()
    {
        _clipsView = NativeView<NativeClip>.MakeNull();
        _clipsBuffer.Dispose();
    }

    private static unsafe NativeView<NativeClip> GetValidateClipBuffer(NativeArray<byte> buffer, int clipCount)
    {
        var view = new NativeView<NativeClip>((NativeClip*)buffer.Ptr, clipCount);
        for (var i = 0; i < clipCount; i++)
        {
            if (view + i == null) Throwers.NullPointer(nameof(buffer));
            var clip = view[i];
            if (clip.IsNull) Throwers.NullPointer(nameof(buffer));
            for (var j = 0; j < clip.Length; j++)
            {
                if (clip.BoneTracks + j == null) Throwers.NullPointer(nameof(buffer));
            }
        }

        return view;
    }
}