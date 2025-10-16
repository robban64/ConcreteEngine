#region

using System.Runtime.CompilerServices;
using ConcreteEngine.Common.Collections;
using ConcreteEngine.Core.Assets.Materials;
using ConcreteEngine.Core.Rendering.Commands;
using ConcreteEngine.Core.Rendering.Data;
using ConcreteEngine.Graphics.Gfx.Resources;

#endregion

namespace ConcreteEngine.Core.Rendering.Registry;

internal sealed class RenderMaterialRegistry
{
    private static int _idx = 0;
    private static MaterialId NextId() => new(++_idx);

    private RenderMaterial?[] _materials = new  RenderMaterial[16];

    private readonly Stack<MaterialId> _free = new();

    public int Count => _idx;

    internal RenderMaterialRegistry()
    {
    }
    
    public RenderMaterial GetMaterial(MaterialId id) => _materials[id - 1]!;

    public void DrainDrawData(Span<DrawMaterialCommand> cmdResult, Span<MaterialParams> paramResult)
    {
        ArgumentOutOfRangeException.ThrowIfNotEqual(paramResult.Length, _idx, nameof(paramResult));

        if(_idx == 0) return;

        var materials = _materials.AsSpan(0, _idx);

        for (var i = 0; i < materials.Length; i++)
        {
            var mat = materials[i];
            if (mat is null)
            {
                cmdResult[i] = default;
                paramResult[i] = default;
                continue;
            }
            cmdResult[i] = new DrawMaterialCommand(mat.Id, mat.ShaderId);
            paramResult[i] = mat.MaterialParams;
        }
    }

    public MaterialId Register(ShaderId shader, in MaterialParams param, ReadOnlySpan<TextureSlotInfo> slots)
    {
        var id = _free.Count > 0 ? _free.Pop() : NextIdAndEnsureCapacity();
        var material = new RenderMaterial(id, shader, in param, slots);
        _materials[id - 1] = material;
        return id;
    }

    public void UpdateMaterial(MaterialId materialId, in MaterialParams param)
    {
        _materials[materialId - 1]?.SetMaterialParams(in param);
    }
    
    public bool TryRemove(MaterialId materialId)
    {
        ArgumentOutOfRangeException.ThrowIfLessThanOrEqual(materialId.Id, 0, nameof(materialId));
        ArgumentOutOfRangeException.ThrowIfGreaterThanOrEqual(materialId.Id, _idx);
        
        var idx = materialId - 1;
        var material = _materials[idx];
        if (material is null || !material.Alive) return false;
        material.Alive = false;
        _free.Push(materialId);
        _materials[idx] = null;
        return true;
    }


    private MaterialId NextIdAndEnsureCapacity()
    {
        var len = _materials.Length;
        if (_idx >= len)
        {
            var newCap = ArrayUtility.CapacityGrowthLinear(len, len * 2, step: 32);

            if (newCap > RenderLimits.MaxMaterialCount)
                throw new InvalidOperationException("Material limit exceeded");

            Array.Resize(ref _materials, newCap);
        }

        return NextId();
    }
}