#region

using System.Diagnostics;
using System.Numerics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using ConcreteEngine.Common.Collections;
using ConcreteEngine.Common.Numerics;
using ConcreteEngine.Engine.Assets;
using ConcreteEngine.Engine.Assets.Models;
using ConcreteEngine.Engine.Worlds.Data;
using ConcreteEngine.Graphics.Gfx.Resources;

#endregion

namespace ConcreteEngine.Engine.Worlds.Render;

public interface IMeshTable
{
    ModelId CreateSimpleModel(MeshId mesh, int materialSlot, int drawCount, in BoundingBox bounds);
    int GetAnimationSlot(ModelId modelId);
}

internal sealed class MeshTable : IMeshTable
{
    private const int DefaultBufferCap = 128;
    private const int DefaultModelCap = 32;
    private const int DefaultAnimationCap = 16;

    private int _modelIdx = 0;
    private ModelId CreateModelId() => new(++_modelIdx);

    private BoundingBox[] _modelBoxes = new BoundingBox[DefaultModelCap];
    private RangeU16[] _modelPartRanges = new RangeU16[DefaultModelCap];

    private MeshPart[] _meshParts = new MeshPart[DefaultBufferCap];
    private BoundingBox[] _partBoxes = new BoundingBox[DefaultBufferCap];
    private Matrix4x4[] _partTransforms = new Matrix4x4[DefaultBufferCap];

    private int[] _animationByModel = new int[DefaultAnimationCap];
    private Matrix4x4[] _modelBoneInvTransform = new Matrix4x4[DefaultAnimationCap];
    private RangeU16[] _modelBoneRanges = new RangeU16[DefaultAnimationCap];
    private Matrix4x4[] _boneTransforms = new Matrix4x4[DefaultBufferCap];

    private readonly List<ModelAnimation> _modelAnimations = new(32);

    private int _partIdx = 0;
    private int _animationIdx = 0;


    public ref readonly BoundingBox GetModelBounds(ModelId id) => ref _modelBoxes[id - 1];

    public ModelPartView GetPartsRefView(ModelId id)
    {
        var index = id - 1;
        if ((uint)index > (uint)_modelPartRanges.Length)
            throw new ArgumentOutOfRangeException(nameof(id));

        var range = _modelPartRanges[index];
        if ((uint)(range.Length + range.Offset) > (uint)_meshParts.Length)
            throw new IndexOutOfRangeException();
        
        var parts = _meshParts.AsSpan(range.Offset, range.Length);
        var locals = _partTransforms.AsSpan(range.Offset, range.Length);
        var boxes = _partBoxes.AsSpan(range.Offset, range.Length);

        return new ModelPartView(parts, locals, boxes, range);
    }


    public int GetAnimationSlot(ModelId modelId) => SortMethod.BinarySearchDataInt(_animationByModel, modelId.Value);

    public ModelAnimationView GetModelAnimationView(int slot)
    {
        if ((uint)slot > (uint)_modelBoneRanges.Length || (uint)slot > (uint)_modelBoneRanges.Length)
            throw new ArgumentOutOfRangeException(nameof(slot));

        var range = _modelBoneRanges[slot];
        if ((uint)(range.Offset + range.Length) > (uint)_boneTransforms.Length)
            throw new IndexOutOfRangeException();

        var boneTransforms = _boneTransforms.AsSpan(range.Offset, range.Length);
        var animations = _modelAnimations[slot];
        return new ModelAnimationView(animations, boneTransforms, ref _modelBoneInvTransform[slot], range);
    }

    public AnimationBonePayload GetBoneUploadPayload()
    {
        if (_animationIdx > _modelBoneRanges.Length)
            throw new IndexOutOfRangeException();
                
        var ranges = _modelBoneRanges.AsSpan(0, _animationIdx);
        var last = ranges[^1];
        var boneTransforms = _boneTransforms.AsSpan(0, last.Offset + last.Length);
        return new AnimationBonePayload(boneTransforms, ranges);
    }


    public ModelId CreateSimpleModel(MeshId mesh, int materialSlot, int drawCount, in BoundingBox bounds)
    {
        EnsureCapacity(_partIdx + 1, _modelIdx + 1);

        _meshParts[_partIdx] = new MeshPart(mesh, materialSlot, drawCount);
        _partTransforms[_partIdx] = Matrix4x4.Identity;
        _modelPartRanges[_modelIdx] = new RangeU16(_partIdx, 1);
        _modelBoxes[_modelIdx] = bounds;

        _partIdx++;
        return new ModelId(++_modelIdx);
    }

    internal void Setup(AssetSystem assets)
    {
        var modelCount = assets.StoreImpl.GetAssetCount<Model>();
        var models = new List<Model>(modelCount);
        assets.StoreImpl.ExtractList<Model, Model>(models, static (it) => it);
        models.Sort();

        int totalParts = 0;
        int animatedModels = 0, totalBones = 0;
        foreach (var model in models)
        {
            totalParts += model.MeshParts.Length;
            if (model.Animation is not null)
            {
                animatedModels++;
                totalBones += model.Animation.BoneCount;
            }
        }

        if (totalParts == 0) return;

        EnsureCapacity(totalParts, models.Capacity);

        var idx = _partIdx;
        for (var i = 0; i < models.Count; i++)
        {
            var model = models[i];
            model.AttachToRenderer(CreateModelId());
            _modelBoxes[i] = model.Bounds;
            _modelPartRanges[i] = new RangeU16(idx, model.MeshParts.Length);
            foreach (var part in model.MeshParts)
            {
                _meshParts[idx] = new MeshPart(part.ResourceId, part.MaterialSlot, part.DrawCount);
                _partTransforms[idx] = part.Transform;
                _partBoxes[idx] = part.Bounds;
                idx++;
            }
        }

        _partIdx = idx;

        if (animatedModels == 0) return;

        EnsureAnimatedCapacity(totalBones, animatedModels);

        idx = _animationIdx;
        for (var i = 0; i < models.Count; i++)
        {
            var model = models[i];
            if (model.Animation is null) continue;

            _modelAnimations.Add(model.Animation);

            var modelBones = model.Animation.GetBoneTransformSpan();
            var boneRangeSpan = _boneTransforms.AsSpan(idx, modelBones.Length);

            modelBones.CopyTo(boneRangeSpan);
            _modelBoneInvTransform[idx] = model.Animation.InverseRootTransform;
            _animationByModel[idx] = model.ModelId;
            _modelBoneRanges[idx] = new RangeU16(idx, modelBones.Length);
            idx += modelBones.Length;
        }

        _animationIdx = idx;
    }

    private void EnsureCapacity(int cap, int rangeCap)
    {
        if (_meshParts.Length != _partTransforms.Length || _meshParts.Length != _partBoxes.Length)
            throw new InvalidOperationException("Mismatch size for model tables");

        if (_meshParts.Length < cap)
        {
            var newCap = ArrayUtility.CapacityGrowthToFit(_meshParts.Length, Math.Max(cap, 64));
            Array.Resize(ref _meshParts, newCap);
            Array.Resize(ref _partTransforms, newCap);
            Array.Resize(ref _partBoxes, newCap);
        }

        if (_modelPartRanges.Length < rangeCap)
        {
            var newCap = ArrayUtility.CapacityGrowthToFit(_modelPartRanges.Length, Math.Max(cap, 64));
            Array.Resize(ref _modelPartRanges, newCap);
            Array.Resize(ref _modelBoxes, newCap);
        }
    }

    private void EnsureAnimatedCapacity(int cap, int rangeCap)
    {
        if (_animationByModel.Length != _modelBoneInvTransform.Length ||
            _animationByModel.Length != _modelBoneRanges.Length)
            throw new InvalidOperationException("Mismatch size for model animation tables");

        if (_boneTransforms.Length < cap)
        {
            var newCap = ArrayUtility.CapacityGrowthToFit(_boneTransforms.Length, Math.Max(cap, 64));
            Array.Resize(ref _boneTransforms, newCap);
        }

        if (_modelBoneRanges.Length < rangeCap)
        {
            var newCap = ArrayUtility.CapacityGrowthToFit(_modelBoneRanges.Length, Math.Max(cap, 64));
            Array.Resize(ref _modelBoneRanges, newCap);
            Array.Resize(ref _animationByModel, newCap);
            Array.Resize(ref _modelBoneInvTransform, newCap);
        }
    }
}