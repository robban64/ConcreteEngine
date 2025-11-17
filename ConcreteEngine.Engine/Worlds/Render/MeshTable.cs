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

    private BoundingBox[] _boundingBoxes = new BoundingBox[DefaultCapacity];

    private MeshPart[] _parts = new MeshPart[DefaultCapacity];
    private Matrix4x4[] _localTransforms = new Matrix4x4[DefaultCapacity];
    private RangeU16[] _partRanges = new RangeU16[DefaultCapacity];

    private int _partIdx = 0;
    private int _modelIdx = 0;

    public ModelPartView GetPartsRefView(ModelId id)
    {
        var range = _partRanges[id - 1];
        var parts = _parts.AsSpan(range.Offset, range.Length);
        var locals = _localTransforms.AsSpan(range.Offset, range.Length);
        return new ModelPartView(parts, locals, range);
    }

    public ModelId CreateModel(MeshId mesh, int materialSlot, int drawCount, in BoundingBox bounds)
    {
        EnsureCapacity(_partIdx + 1, _modelIdx + 1);

        _boundingBoxes[_modelIdx] = bounds;

        _parts[_partIdx] = new MeshPart(mesh, materialSlot, drawCount);
        _localTransforms[_partIdx] = Matrix4x4.Identity;
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
            _boundingBoxes[i] = model.Bounds;
            _partRanges[i] = new RangeU16((ushort)idx, (ushort)model.MeshParts.Length);
            foreach (var part in model.MeshParts)
            {
                _parts[idx] = new MeshPart(part.ResourceId, part.MaterialSlot, part.DrawCount);
                _localTransforms[idx] = part.Transform;
                idx++;
            }
        }

        _partIdx = idx;
    }

    private void EnsureCapacity(int cap, int rangeCap)
    {
        Debug.Assert(_parts.Length == _localTransforms.Length);

        if (_parts.Length < cap)
        {
            var newCap = ArrayUtility.CapacityGrowthToFit(_parts.Length, Math.Max(cap, 64));
            Array.Resize(ref _parts, newCap);
            Array.Resize(ref _localTransforms, newCap);
        }

        if (_partRanges.Length < rangeCap)
        {
            var newCap = ArrayUtility.CapacityGrowthToFit(_partRanges.Length, Math.Max(cap, 64));
            Array.Resize(ref _partRanges, newCap);
        }
    }
}