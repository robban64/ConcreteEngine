#region

using System.Diagnostics;
using System.Numerics;
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
}

internal sealed class MeshTable : IMeshTable
{
    private const int DefaultBufferCap = 128;
    private const int DefaultModelCap = 32;
    private const int DefaultAnimationCap = 16;
    
    private int _modelIdx = 0;
    private ModelId CreateModelId() => new (++_modelIdx);

    private BoundingBox[] _modelBoxes = new BoundingBox[DefaultModelCap];
    private RangeU16[] _modelPartRanges = new RangeU16[DefaultModelCap];

    private MeshPart[] _meshParts = new MeshPart[DefaultBufferCap];
    private BoundingBox[] _partBoxes = new  BoundingBox[DefaultBufferCap];
    private Matrix4x4[] _partTransforms = new Matrix4x4[DefaultBufferCap];
    
    private Matrix4x4[] _modelBoneInvTransform = new Matrix4x4[DefaultAnimationCap];
    private RangeU16[] _modelBoneRanges = new RangeU16[DefaultAnimationCap];
    private Matrix4x4[] _boneTransforms = new Matrix4x4[DefaultBufferCap];

    private int _partIdx = 0;
    private int _boneRangeIdx = 0;


    public ref readonly BoundingBox GetModelBounds(ModelId id) => ref _modelBoxes[id - 1];

    public ModelPartView GetPartsRefView(ModelId id)
    {
        var range = _modelPartRanges[id - 1];
        var parts = _meshParts.AsSpan(range.Offset, range.Length);
        var locals = _partTransforms.AsSpan(range.Offset, range.Length);
        var boxes = _partBoxes.AsSpan(range.Offset, range.Length);

        return new ModelPartView(parts, locals, boxes, range);
    }

    public ModelId CreateSimpleModel(MeshId mesh, int materialSlot, int drawCount, in BoundingBox bounds)
    {
        EnsureCapacity(_partIdx + 1, _modelIdx + 1);

        _modelBoxes[_modelIdx] = bounds;

        _meshParts[_partIdx] = new MeshPart(mesh, materialSlot, drawCount);
        _partTransforms[_partIdx] = Matrix4x4.Identity;
        _modelPartRanges[_modelIdx] = new RangeU16(_partIdx, 1);

        _partIdx++;
        return new ModelId(++_modelIdx);
    }

    internal void Setup(AssetSystem assets)
    {
        var modelCount = assets.StoreImpl.GetAssetCount<Model>();
        var models = new List<Model>(modelCount);
        assets.StoreImpl.ExtractList<Model, Model>(models, static (it) => it);
        models.Sort();

        int totalParts = 0, animatedModels  = 0, totalBones = 0;
        foreach (var model in models)
        {
            totalParts += model.MeshParts.Length;
            if (model.Animation is not null)
            {
                animatedModels++;
                totalBones += model.Animation.BoneCount;
            }
        }
        
        if(totalParts == 0) return;

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
        
        if(animatedModels == 0) return;

        EnsureAnimatedCapacity(totalBones, animatedModels);

        idx = _boneRangeIdx;
        for (var i = 0; i < models.Count; i++)
        {
            var model = models[i];
            if(model.Animation is null) continue;

            var modelBones = model.Animation.GetBoneTransformSpan();
            var boneRangeSpan = _boneTransforms.AsSpan(idx, modelBones.Length);
            
            modelBones.CopyTo(boneRangeSpan);
            _modelBoneInvTransform[i] = model.Animation.InverseRootTransform;
            _modelBoneRanges[i] = new RangeU16(idx, modelBones.Length);
            idx +=  modelBones.Length;
        }

        _boneRangeIdx = idx;

    }



    private void EnsureCapacity(int cap, int rangeCap)
    {
        Debug.Assert(_meshParts.Length == _partTransforms.Length);

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
        if (_boneTransforms.Length < cap)
        {
            var newCap = ArrayUtility.CapacityGrowthToFit(_boneTransforms.Length, Math.Max(cap, 64));
            Array.Resize(ref _boneTransforms, newCap);
        }

        if (_modelBoneRanges.Length < rangeCap)
        {
            var newCap = ArrayUtility.CapacityGrowthToFit(_modelBoneRanges.Length, Math.Max(cap, 64));
            Array.Resize(ref _modelBoneRanges, newCap);
        }
    }
}