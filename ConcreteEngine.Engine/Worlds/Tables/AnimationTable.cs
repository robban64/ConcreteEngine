using System.Numerics;
using ConcreteEngine.Common.Collections;
using ConcreteEngine.Engine.Assets;
using ConcreteEngine.Engine.Assets.Models;
using ConcreteEngine.Engine.Editor.Diagnostics;
using ConcreteEngine.Engine.Worlds.Data;
using ConcreteEngine.Engine.Worlds.Render.Data;
using ConcreteEngine.Renderer.Data;
using ConcreteEngine.Shared.Diagnostics;

namespace ConcreteEngine.Engine.Worlds.Tables;

internal sealed class AnimationTable
{
    private const int DefaultAnimatedModelCap = 64;
    private const int DefaultBoneBufferCap = 64 * RenderLimits.BoneCapacity;

    private static AnimationId MakeId() => new(++_idx);
    private static int _idx = 0;

    private ModelId[] _idxToModel = new ModelId[DefaultAnimatedModelCap];
    private BoneTrack[][][] _clips = new BoneTrack[DefaultAnimatedModelCap][][];
    private Matrix4x4[] _modelBoneInvTransform = new Matrix4x4[DefaultAnimatedModelCap];

    private int[] _parentIndices = new int[DefaultBoneBufferCap];
    private Matrix4x4[] _boneOffsetMatrix = new Matrix4x4[DefaultBoneBufferCap];
    private Matrix4x4[] _nodeTransform = new Matrix4x4[DefaultBoneBufferCap];

    public int TotalBones { get; private set; }
    public int TotalClips { get; private set; }


    public int Count => _idx;

    public ReadOnlySpan<ModelId> ModelIdSpan => _idxToModel;

    public AnimationDataView GetDataView() =>
        new(_clips, _boneOffsetMatrix, _nodeTransform, _parentIndices, _modelBoneInvTransform);

    public int GetClipCount(AnimationId animation)
    {
        int index = animation - 1;
        if ((uint)index >= (uint)_clips.Length) throw new IndexOutOfRangeException();
        return _clips[index].Length;
    }

    internal void Setup(AssetSystem assets)
    {
        _idx = 0;

        var models = new List<Model>(8);
        assets.Store.ExtractList<Model, Model>(models, static (it) => it.Animation != null ? it : null!);
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

            Logger.LogString(LogScope.World, $"Animation table bone buffer resize {newCap}", LogLevel.Warn);
        }

        if (_modelBoneInvTransform.Length < animationCap)
        {
            var newCap =
                Arrays.CapacityGrowthSafe(_modelBoneInvTransform.Length, animationCap, Arrays.TableSmallThreshold);

            Array.Resize(ref _idxToModel, newCap);
            Array.Resize(ref _modelBoneInvTransform, newCap);
            Array.Resize(ref _clips, newCap);
            Logger.LogString(LogScope.World, $"Animation table clip resize {newCap}", LogLevel.Warn);
        }
    }
}