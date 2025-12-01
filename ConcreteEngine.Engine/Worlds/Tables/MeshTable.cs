#region

using System.Numerics;
using ConcreteEngine.Common.Collections;
using ConcreteEngine.Common.Numerics;
using ConcreteEngine.Engine.Assets;
using ConcreteEngine.Engine.Assets.Models;
using ConcreteEngine.Engine.Worlds.Data;
using ConcreteEngine.Engine.Worlds.Render.Data;
using ConcreteEngine.Graphics.Gfx.Resources;

#endregion

namespace ConcreteEngine.Engine.Worlds.Tables;

public interface IMeshTable
{
    ModelId CreateSimpleModel(MeshId mesh, int materialSlot, int drawCount, in BoundingBox bounds);
    //int GetAnimationSlot(ModelId modelId);
}

internal sealed class MeshTable : IMeshTable
{
    private const int DefaultPartCap = 128;
    private const int DefaultModelCap = 64;

    private int _modelIdx = 0;
    private ModelId CreateModelId() => new(++_modelIdx);

    private BoundingBox[] _modelBoxes = new BoundingBox[DefaultModelCap];
    private RangeU16[] _modelPartRanges = new RangeU16[DefaultModelCap];

    private MeshPart[] _meshParts = new MeshPart[DefaultPartCap];
    private BoundingBox[] _partBoxes = new BoundingBox[DefaultPartCap];
    private Matrix4x4[] _partTransforms = new Matrix4x4[DefaultPartCap];


    private int _partIdx = 0;

    
    public ModelBoundsView GetModelBoundSpan() => new(_modelBoxes);


    public ushort GetPartLengthFor(ModelId id)
    {
        var index = id - 1;
        if ((uint)index > (uint)_modelPartRanges.Length)
            throw new ArgumentOutOfRangeException(nameof(id));

        return _modelPartRanges[index].Length;
    }

    public ReadOnlySpan<Matrix4x4> GetPartTransforms(ModelId id)
    {
        var index = id - 1;
        if ((uint)index >= _modelPartRanges.Length)
            throw new ArgumentOutOfRangeException(nameof(id));

        var range = _modelPartRanges[index];
        return _partTransforms.AsSpan(range.Offset, range.Length);
    }
    
    public ReadOnlySpan<MeshPart> GetMeshParts(ModelId id)
    {
        var index = id - 1;
        if ((uint)index >= _modelPartRanges.Length)
            throw new ArgumentOutOfRangeException(nameof(id));

        var range = _modelPartRanges[index];
        if ((uint)(range.Length + range.Offset) > _meshParts.Length)
            throw new IndexOutOfRangeException();
        
        return _meshParts.AsSpan(range.Offset, range.Length);
    }


    public ModelPartView GetPartsRefView(ModelId id)
    {
        var index = id - 1;
        if ((uint)index >= _modelPartRanges.Length)
            throw new ArgumentOutOfRangeException(nameof(id));

        var range = _modelPartRanges[index];
        if ((uint)(range.Length + range.Offset) > _meshParts.Length)
            throw new IndexOutOfRangeException();

        var parts = _meshParts.AsSpan(range.Offset, range.Length);
        var locals = _partTransforms.AsSpan(range.Offset, range.Length);
        var boxes = _partBoxes.AsSpan(range.Offset, range.Length);

        return new ModelPartView(parts, locals, boxes);
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
        foreach (var model in models) totalParts += model.MeshParts.Length;

        if (totalParts == 0) return;

        EnsureCapacity(totalParts, models.Capacity);

        var idx = _partIdx;
        for (var i = 0; i < models.Count; i++)
        {
            var model = models[i];
            model.AttachModel(CreateModelId());
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
    }

    private void EnsureCapacity(int cap, int rangeCap)
    {
        if (_meshParts.Length != _partTransforms.Length || _meshParts.Length != _partBoxes.Length)
            throw new InvalidOperationException("Mismatch size for model tables");

        if (_meshParts.Length < cap)
        {
            var newCap = Arrays.CapacityGrowthSafe(_meshParts.Length, cap);
            Array.Resize(ref _meshParts, newCap);
            Array.Resize(ref _partTransforms, newCap);
            Array.Resize(ref _partBoxes, newCap);
            Console.WriteLine("_meshParts resize");
        }

        if (_modelPartRanges.Length < rangeCap)
        {
            var newCap = Arrays.CapacityGrowthSafe(_modelPartRanges.Length, rangeCap, Arrays.TableSmallThreshold);
            Array.Resize(ref _modelPartRanges, newCap);
            Array.Resize(ref _modelBoxes, newCap);
            Console.WriteLine("_modelPartRanges resize");
        }
    }
}