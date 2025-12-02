#region

using System.Numerics;
using ConcreteEngine.Common.Collections;
using ConcreteEngine.Engine.Assets;
using ConcreteEngine.Engine.Assets.Models;
using ConcreteEngine.Engine.Worlds.Data;
using ConcreteEngine.Engine.Worlds.Render.Data;
using ConcreteEngine.Renderer.Data;

#endregion

namespace ConcreteEngine.Engine.Worlds.Tables;

internal sealed class AnimationTable
{
    private const int DefaultAnimatedModelCap = 64;
    private const int DefaultBoneBufferCap = 64 * RenderLimits.BoneCapacity;

    private AnimationId MakeId() => new(++_idx);
    private int _idx = 0;

    private int[] _idxToModel = new int[DefaultAnimatedModelCap];
    private BoneTrack[][][] _clips = new BoneTrack[DefaultAnimatedModelCap][][];
    private Matrix4x4[] _modelBoneInvTransform = new Matrix4x4[DefaultAnimatedModelCap];

    private int[] _parentIndices = new int[DefaultBoneBufferCap];
    private Matrix4x4[] _boneOffsetMatrix = new Matrix4x4[DefaultBoneBufferCap];
    private Matrix4x4[] _nodeTransform = new Matrix4x4[DefaultBoneBufferCap];

    public int TotalBones { get; private set; }
    public int TotalClips { get; private set; }

    public AnimationDataView GetDataView() =>
        new(_clips, _boneOffsetMatrix, _nodeTransform, _parentIndices, _modelBoneInvTransform);

    /*
    public ModelAnimationView GetModelAnimationView(AnimationId animation)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(animation.Value);

        const int boneCap = RenderLimits.BoneCapacity;

        var index = animation - 1;
        if (index == -1 || (uint)index > _clips.Length || (uint)index > _modelBoneInvTransform.Length)
            throw new IndexOutOfRangeException();

        var startOffset = index * boneCap;

        if ((uint)(startOffset + boneCap) > (uint)_boneOffsetMatrix.Length ||
            (uint)(startOffset + boneCap) > (uint)_nodeTransform.Length)
        {
            throw new IndexOutOfRangeException();
        }

        var boneTransforms = _boneOffsetMatrix.AsSpan(startOffset, boneCap);
        var nodes = _nodeTransform.AsSpan(startOffset, boneCap);
        var indices = _parentIndices.AsSpan(startOffset, boneCap);
        var clip = _clips[index];
        return new ModelAnimationView(clip, boneTransforms, nodes, indices, ref _modelBoneInvTransform[index]);
    }*/

    internal void Setup(AssetSystem assets)
    {
        _idx = 0;

        var models = new List<Model>(8);
        assets.StoreImpl.ExtractList<Model, Model>(models, static (it) => it.Animation != null ? it : null!);
        models.Sort();

        int totalBones = 0, totalClips = 0, modelHighId = -1;
        foreach (var model in models)
        {
            totalBones += model.Animation!.BoneOffsetMatrixSpan.Length;
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
            var modelBones = animation.BoneOffsetMatrixSpan;
            var modelClips = animation.ClipDataSpan;

            var animationId = MakeId();
            model.AttachAnimation(animationId);

            var tableBones = _boneOffsetMatrix.AsSpan(boneOffset, modelBones.Length);
            var tableNodes = _nodeTransform.AsSpan(boneOffset, modelBones.Length);
            var tableIndices = _parentIndices.AsSpan(boneOffset, modelBones.Length);
            _idxToModel[i] = model.ModelId;
            _modelBoneInvTransform[i] = model.Animation!.InverseRootTransform;

            //_modelBoneRanges[i] = new RangeU16(boneOffset, modelBones.Length);
            modelBones.CopyTo(tableBones);
            animation.NodeTransformSpan.CopyTo(tableNodes);
            animation.ParentIndexSpan.CopyTo(tableIndices);

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

                    clips[c][t] = new BoneTrack(track.Positions, track.PositionTimes, track.Rotations,
                        track.RotationTimes, track.Scales, track.ScaleTimes);
                }
            }

            _clips[i] = clips;

            boneOffset += RenderLimits.BoneCapacity;
        }
    }


    private void EnsureAnimatedCapacity(int boneCap, int animationCap)
    {
        if (_idxToModel.Length != _modelBoneInvTransform.Length)
            throw new InvalidOperationException("Mismatch size for model animation tables");

        if (_boneOffsetMatrix.Length != _nodeTransform.Length)
            throw new InvalidOperationException("Mismatch size between bone and node in animation tables");

        if (_boneOffsetMatrix.Length < boneCap)
        {
            var newCap = Arrays.CapacityGrowthSafe(_boneOffsetMatrix.Length, boneCap);
            Array.Resize(ref _boneOffsetMatrix, newCap);
            Array.Resize(ref _nodeTransform, newCap);
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