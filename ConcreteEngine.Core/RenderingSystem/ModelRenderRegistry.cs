using System.Diagnostics;
using System.Numerics;
using ConcreteEngine.Common.Collections;
using ConcreteEngine.Common.Numerics;
using ConcreteEngine.Core.Assets;
using ConcreteEngine.Core.Assets.Meshes;
using ConcreteEngine.Core.Scene.Entities;
using ConcreteEngine.Graphics.Gfx.Resources;
using ConcreteEngine.Renderer.Data;

namespace ConcreteEngine.Core.RenderingSystem;

public readonly record struct ModelId(int Value)
{
    public static implicit operator int(ModelId id) => id.Value;
}

public readonly struct MeshPart(MeshId mesh, int drawCount)
{
    public readonly MeshId Mesh = mesh;
    public readonly int DrawCount = drawCount;
}

public readonly ref struct ModelPartView(ReadOnlySpan<MeshPart> parts, ReadOnlySpan<Matrix4x4> locals, RangeU16 ranges)
{
    public readonly ReadOnlySpan<MeshPart> Parts = parts;
    public readonly ReadOnlySpan<Matrix4x4> Locals = locals;
    public readonly RangeU16 Range = ranges;
}

internal sealed class ModelRenderRegistry
{
    private const int DefaultCapacity = 128;

    private MeshPart[] _parts = new MeshPart[DefaultCapacity];
    private Matrix4x4[] _localTransforms = new Matrix4x4[DefaultCapacity];
    private RangeU16[] _partRanges = new RangeU16[DefaultCapacity];

    //private MaterialId[] _materials = new MaterialId[DefaultCapacity];

    public ModelPartView GetParts(ModelId id)
    {
        var range = _partRanges[id - 1];
        var parts = _parts.AsSpan(range.Offset, range.Length);
        var locals = _localTransforms.AsSpan(range.Offset, range.Length);
        return new ModelPartView(parts, locals, range);
    }


    internal void Setup(AssetSystem assets)
    {
        var modelCount = assets.StoreImpl.GetAssetCount<Model>();
        var models = new List<Model>(modelCount);
        assets.StoreImpl.ExtractList<Model, Model>(models, static (it) => it);
        models.Sort();

        var totalParts = 0;
        foreach (var meshes in models) totalParts += meshes.MeshParts.Length;

        EnsureCapacity(totalParts, models.Capacity);

        var idx = 0;
        for (int i = 0; i < models.Count; i++)
        {
            var meshes = models[i];
            _partRanges[i] = new RangeU16((ushort)idx, (ushort)meshes.MeshParts.Length);
            foreach (var part in meshes.MeshParts)
            {
                _parts[idx] = new MeshPart(part.ResourceId, part.DrawCount);
                _localTransforms[idx] = part.Transform;
                idx++;
            }
        }
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