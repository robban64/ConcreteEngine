using System.Diagnostics;
using System.Numerics;
using ConcreteEngine.Common.Collections;
using ConcreteEngine.Common.Numerics;
using ConcreteEngine.Core.Assets;
using ConcreteEngine.Core.Assets.Meshes;
using ConcreteEngine.Core.RenderingSystem.Data;
using ConcreteEngine.Core.Scene.Entities;
using ConcreteEngine.Graphics.Gfx.Resources;
using ConcreteEngine.Renderer.Data;

namespace ConcreteEngine.Core.RenderingSystem;

public interface IModelRegistry
{
    ModelId CreateModel(MeshId mesh, int drawCount);
}

internal sealed class ModelRegistry : IModelRegistry
{
    private const int DefaultCapacity = 128;
    
    private MeshPart[] _parts = new MeshPart[DefaultCapacity];
    private Matrix4x4[] _localTransforms = new Matrix4x4[DefaultCapacity];
    private RangeU16[] _partRanges = new RangeU16[DefaultCapacity];

    //private MaterialId[] _materials = new MaterialId[DefaultCapacity];
    
    private int _modelIdx = 0;
    private int _idx = 0;
    
    public ModelPartView GetPartsView(ModelId id)
    {
        var range = _partRanges[id - 1];
        var parts = _parts.AsSpan(range.Offset, range.Length);
        var locals = _localTransforms.AsSpan(range.Offset, range.Length);
        return new ModelPartView(parts, locals, range);
    }

    public ModelId CreateModel(MeshId mesh, int drawCount)
    {
        EnsureCapacity(_idx + 1, _modelIdx + 1);
        
        _parts[_idx] = new MeshPart(mesh, drawCount);
        _localTransforms[_idx] = Matrix4x4.Identity;
        _partRanges[_modelIdx] = new RangeU16((ushort)_idx, 1);

        _idx++;
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

        var idx = _idx;
        for (int i = 0; i < models.Count; i++)
        {
            var model = models[i];
            model.AttachToRenderer(new ModelId(++_modelIdx));
            _partRanges[i] = new RangeU16((ushort)idx, (ushort)model.MeshParts.Length);
            foreach (var part in model.MeshParts)
            {
                _parts[idx] = new MeshPart(part.ResourceId, part.DrawCount);
                _localTransforms[idx] = part.Transform;
                idx++;
            }
        }
        _idx = idx;
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