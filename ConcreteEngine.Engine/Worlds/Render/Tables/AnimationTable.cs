using System.Numerics;
using ConcreteEngine.Common.Collections;
using ConcreteEngine.Common.Numerics;
using ConcreteEngine.Engine.Assets;
using ConcreteEngine.Engine.Assets.Models;
using ConcreteEngine.Engine.Worlds.Data;
using ConcreteEngine.Engine.Worlds.Render.Data;

namespace ConcreteEngine.Engine.Worlds.Tables;

public unsafe struct AnimationClipData
{
    public float Duration;
    public float TicksPerSecond;
    public fixed ushort BoneTrack[64];
}

internal sealed class AnimationTable
{
    public const int BoneCap = 64;

    private const int DefaultAnimatedModelCap = 64;
    private const int DefaultBoneBufferCap = 64 * DefaultAnimatedModelCap;

    private static AnimationId MakeId() => new (++_idx);
    private static int _idx = 0;

    //private RangeU16[] _modelBoneRanges = new RangeU16[DefaultClipCap];
    private int[] _idxToModel = new int[DefaultAnimatedModelCap];
    private BoneTrack[][][] _clips = new BoneTrack[DefaultAnimatedModelCap][][];
    private Matrix4x4[] _modelBoneInvTransform = new Matrix4x4[DefaultAnimatedModelCap];

    private int[] _parentIndices = new int[DefaultBoneBufferCap];
    private Matrix4x4[] _boneTransforms = new Matrix4x4[DefaultBoneBufferCap];
    private Matrix4x4[] _nodeTransforms = new Matrix4x4[DefaultBoneBufferCap];

    // private RangeU16[] _clipTrackRanges = new RangeU16[DefaultClipCap];
    //private BoneClipTrack[] _clipBoneTrack = new BoneClipTrack[DefaultBoneBufferCap];

    public int TotalBones { get; private set; }
    public int TotalClips { get; private set; }

    public ModelAnimationView GetModelAnimationView(AnimationId animation)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(animation.Value);

        var index = animation - 1;
        if (index == -1 || (uint)index > _clips.Length || (uint)index > _modelBoneInvTransform.Length)
            throw new IndexOutOfRangeException();

        var startOffset = index * BoneCap;

        if ((uint)(startOffset + BoneCap) > (uint)_boneTransforms.Length ||
            (uint)(startOffset + BoneCap) > (uint)_nodeTransforms.Length)
        {
            throw new IndexOutOfRangeException();
        }

        var boneTransforms = _boneTransforms.AsSpan(startOffset, BoneCap);
        var nodes = _nodeTransforms.AsSpan(startOffset, BoneCap);
        var indices = _parentIndices.AsSpan(startOffset, BoneCap);
        var clip = _clips[index];
        return new ModelAnimationView(clip, boneTransforms, nodes, indices, ref _modelBoneInvTransform[index]);
    }

    internal void Setup(AssetSystem assets)
    {
        _idx = 0;

        var models = new List<Model>(8);
        assets.StoreImpl.ExtractList<Model, Model>(models, static (it) => it.Animation != null ? it : null!);
        models.Sort();

        int totalBones = 0, totalClips = 0, modelHighId = -1;
        foreach (var model in models)
        {
            totalBones += model.Animation!.BoneTransforms.Length;
            totalClips += model.Animation.ClipDataSpan.Length;
            modelHighId = int.Max(modelHighId, model.ModelId);
        }

        TotalBones = totalBones;
        TotalClips = totalClips;
        if (TotalBones == 0 || TotalClips == 0) return;

        EnsureAnimatedCapacity(TotalBones, TotalClips);

        int boneOffset = 0;
        int clipIdx = 0;
        for (var i = 0; i < models.Count; i++)
        {
            var model = models[i];
            var animation = model.Animation!;
            var modelBones = animation.BoneTransforms;
            var modelClips = animation.ClipDataSpan;

            var animationId = MakeId();
            model.AttachAnimation(animationId);

            var tableBones = _boneTransforms.AsSpan(boneOffset, modelBones.Length);
            var tableNodes = _nodeTransforms.AsSpan(boneOffset, modelBones.Length);
            var tableIndices = _parentIndices.AsSpan(boneOffset, modelBones.Length);
            _idxToModel[i] = model.ModelId;
            _modelBoneInvTransform[i] = model.Animation!.InverseRootTransform;

            //_modelBoneRanges[i] = new RangeU16(boneOffset, modelBones.Length);
            modelBones.CopyTo(tableBones);
            animation.NodeTransforms.CopyTo(tableNodes);
            animation.ParentIndices.CopyTo(tableIndices);

            var clips = new BoneTrack[modelClips.Length][];

            for (int c = 0; c < clips.Length; c++)
            {
                var animationClip = modelClips[c];
                clips[c] = new BoneTrack[animation.DefinedBoneCount];
                for (int t = 0; t < animation.DefinedBoneCount; t++)
                {
                    if (!animationClip.Tracks.TryGetValue(t, out var track))
                    {
                        clips[c][t] = new BoneTrack();
                        continue;
                    }
                    clips[c][t] = new BoneTrack(track.Translations, track.TranslationTimes, track.Rotations,
                        track.RotationTimes, track.Scales, track.ScaleTimes);
                }
            }

            _clips[i] = clips;

            boneOffset += BoneCap;

            /*
            var animationClip = model.Animation!.ClipDataSpan;
            var tableClips = _clips.AsSpan(clipTrackOffset, animationClip.Length);
            _clipRanges[i] = new RangeU16(clipTrackOffset, animationClip.Length);
            animationClip.CopyTo(tableClips);

            clipTrackOffset += animationClip.Length;*/


            /*
            var animationClip = model.Animation!.ClipDataSpan;
            var tableClip = _clipBoneTrack.AsSpan(clipTrackOffset, animationClip.Length);
            _clipRanges[i] = new RangeU16(clipTrackOffset, animationClip.Length);
            clipTrackOffset += animationClip.Length;

            int localClipIdx = 0;
            foreach (var clip in animationClip)
            {
                _clipTrackRanges[i+localClipIdx] = new RangeU16()
                tableClip[localClipIdx++]
            }*/
        }
    }


    private void EnsureAnimatedCapacity(int boneCap, int animationCap)
    {
        if (_idxToModel.Length != _modelBoneInvTransform.Length)
            throw new InvalidOperationException("Mismatch size for model animation tables");

        if (_boneTransforms.Length != _nodeTransforms.Length)
            throw new InvalidOperationException("Mismatch size between bone and node in animation tables");

        if (_boneTransforms.Length < boneCap)
        {
            var newCap = Arrays.CapacityGrowthSafe(_boneTransforms.Length, boneCap);
            Array.Resize(ref _boneTransforms, newCap);
            Array.Resize(ref _nodeTransforms, newCap);
            Array.Resize(ref _parentIndices, newCap);

            Console.WriteLine("animation bones resize");
        }

        if (_modelBoneInvTransform.Length < animationCap)
        {
            var newCap =
                Arrays.CapacityGrowthSafe(_modelBoneInvTransform.Length, animationCap, Arrays.TableSmallThreshold);

            Array.Resize(ref _idxToModel, newCap);
            Array.Resize(ref _modelBoneInvTransform, newCap);
            Array.Resize(ref _clips, newCap);

            Console.WriteLine("animation clips resize");
        }
    }
}