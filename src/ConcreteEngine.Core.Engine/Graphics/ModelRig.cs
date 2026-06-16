using System.Collections.ObjectModel;
using System.Numerics;
using System.Runtime.CompilerServices;
using ConcreteEngine.Core.Common;
using ConcreteEngine.Core.Common.Memory;

namespace ConcreteEngine.Core.Engine.Graphics;

public sealed class ModelRig : IDisposable
{
    private static ushort _idCounter;

    public readonly Id16<ModelRig> Id = new(++_idCounter);
    public readonly int ClipCount;
    public readonly int BoneCount;

    public readonly ReadOnlyDictionary<string, int> BoneMapping;

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
            if(clip == null!) Throwers.InvalidArgument(nameof(clips));
        }

        ClipCount = clips.Length;
        BoneCount = parentIndices.Length;

        BoneMapping = new ReadOnlyDictionary<string, int>(boneMapping);
        _clips = clips.ToArray();
        _parentIndices = parentIndices.ToArray();
        _bindPose = bindPose.ToArray();
        _inverseBindPose = inverseBindPose.ToArray();
        _clipsBuffer = clipsBuffer;
        _clipsView = GetValidateClipBuffer(_clipsBuffer, ClipCount);
    }

    public AnimationClip GetClip(int index) => _clips[index];

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal SkinningContext GetSkinningContext(int clip)
    {
        if (_clipsView.IsNull || (uint)clip >= (uint)ClipCount)
            Throwers.InvalidOperation(nameof(_clipsBuffer));

        return new SkinningContext(_parentIndices, _bindPose, _inverseBindPose, _clipsView[clip]);
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
            if (clip.IsNull || clip.BoneTracks.IsNull) Throwers.NullPointer(nameof(buffer));
            for (var j = 0; j < clip.Length; j++)
            {
                if (clip.BoneTracks + j == null) Throwers.NullPointer(nameof(buffer));
            }
        }

        return view;
    }
}
