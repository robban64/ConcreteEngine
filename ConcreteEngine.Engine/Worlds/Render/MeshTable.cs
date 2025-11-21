#region

using System.Diagnostics;
using System.Numerics;
using ConcreteEngine.Common.Collections;
using ConcreteEngine.Common.Numerics;
using ConcreteEngine.Engine.Assets;
using ConcreteEngine.Engine.Assets.Meshes;
using ConcreteEngine.Engine.Worlds.Data;
using ConcreteEngine.Graphics.Gfx.Resources;

#endregion

namespace ConcreteEngine.Engine.Worlds.Render;

public interface IMeshTable
{
    ModelId CreateModel(MeshId mesh, int materialSlot, int drawCount, in BoundingBox bounds);
}

internal sealed class MeshTable : IMeshTable
{
    private const int DefaultCapacity = 128;

    private BoundingBox[] _modelBoxes = new BoundingBox[DefaultCapacity];
    private RangeU16[] _partRanges = new RangeU16[DefaultCapacity];

    private MeshPart[] _meshParts = new MeshPart[DefaultCapacity];
    private BoundingBox[] _partBoxes = new  BoundingBox[DefaultCapacity];
    private Matrix4x4[] _partTransforms = new Matrix4x4[DefaultCapacity];
    
    private RangeU16[] _boneRanges = new RangeU16[DefaultCapacity];
    private Matrix4x4[] _boneTransforms = new Matrix4x4[DefaultCapacity];

    private int _partIdx = 0;
    private int _modelIdx = 0;

    public ref readonly BoundingBox GetModelBounds(ModelId id) => ref _modelBoxes[id - 1];

    public ModelPartView GetPartsRefView(ModelId id)
    {
        var range = _partRanges[id - 1];
        var parts = _meshParts.AsSpan(range.Offset, range.Length);
        var locals = _partTransforms.AsSpan(range.Offset, range.Length);
        var boxes = _partBoxes.AsSpan(range.Offset, range.Length);

        return new ModelPartView(parts, locals, boxes, range);
    }

    public ModelId CreateModel(MeshId mesh, int materialSlot, int drawCount, in BoundingBox bounds)
    {
        EnsureCapacity(_partIdx + 1, _modelIdx + 1);

        _modelBoxes[_modelIdx] = bounds;

        _meshParts[_partIdx] = new MeshPart(mesh, materialSlot, drawCount);
        _partTransforms[_partIdx] = Matrix4x4.Identity;
        _partRanges[_modelIdx] = new RangeU16((ushort)_partIdx, 1);

        _partIdx++;
        return new ModelId(++_modelIdx);
    }

    internal void Setup(AssetSystem assets)
    {
        var modelCount = assets.StoreImpl.GetAssetCount<Model>();
        var models = new List<Model>(modelCount);
        assets.StoreImpl.ExtractList<Model, Model>(models, static (it) => it);
        models.Sort();

        var totalParts = 0;
        foreach (var model in models) totalParts += model.MeshParts.Length;

        EnsureCapacity(totalParts, models.Capacity);

        var idx = _partIdx;
        for (var i = 0; i < models.Count; i++)
        {
            var model = models[i];
            model.AttachToRenderer(new ModelId(++_modelIdx));
            _modelBoxes[i] = model.Bounds;
            _partRanges[i] = new RangeU16((ushort)idx, (ushort)model.MeshParts.Length);
            foreach (var part in model.MeshParts)
            {
                _meshParts[idx] = new MeshPart(part.ResourceId, part.MaterialSlot, part.DrawCount);
                _partTransforms[idx] = part.Transform;
                _partBoxes[idx] = part.Bounds;
                idx++;
            }
        }

        _partIdx = idx;
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

        if (_partRanges.Length < rangeCap)
        {
            var newCap = ArrayUtility.CapacityGrowthToFit(_partRanges.Length, Math.Max(cap, 64));
            Array.Resize(ref _partRanges, newCap);
            Array.Resize(ref _modelBoxes, newCap);
        }
    }
}