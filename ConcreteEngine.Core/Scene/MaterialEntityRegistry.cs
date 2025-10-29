using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using ConcreteEngine.Common.Collections;
using ConcreteEngine.Core.RenderingSystem;
using ConcreteEngine.Core.RenderingSystem.Data;
using ConcreteEngine.Core.Scene.Entities;
using ConcreteEngine.Renderer.Data;

namespace ConcreteEngine.Core.Scene;


public interface IMaterialEntityRegistry
{
    MaterialTagKey Add(MaterialId m0, MaterialId m1 = default, MaterialId m2 = default, MaterialId m3 = default,
        MaterialId m4 = default, MaterialId m5 = default, MaterialId m6 = default, MaterialId m7 = default);
    
    MaterialTagKey AddSpan(ReadOnlySpan<MaterialId> s);

}

public sealed class MaterialEntityRegistry : IMaterialEntityRegistry
{
    private int _keyIdx = 0;
    private MaterialTag[] _table = new MaterialTag[128];

    public MaterialTagKey Add(MaterialId m0, MaterialId m1 = default, MaterialId m2 = default, MaterialId m3 = default,
        MaterialId m4 = default, MaterialId m5 = default, MaterialId m6 = default, MaterialId m7 = default)
    {
        ArgumentOutOfRangeException.ThrowIfLessThanOrEqual(m0.Id, 0);
        EnsureCapacity(1);
        _table[_keyIdx++] = new MaterialTag(m0, m1, m2, m3, m4, m5, m6);
        return new MaterialTagKey(_keyIdx);
    }

    public MaterialTagKey AddSpan(ReadOnlySpan<MaterialId> s)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(s.Length, 8, nameof(s));
        EnsureCapacity(1);
        _table[_keyIdx++] = new MaterialTag(s[0], s[1], s[2], s[3], s[4], s[5], s[6]);
        return new MaterialTagKey(_keyIdx);
    }

    private void EnsureCapacity(int n)
    {
        var newSize = _keyIdx + n;
        if(newSize >= _table.Length)
            Array.Resize(ref _table, ArrayUtility.CapacityGrowthLinear(_keyIdx, newSize, 32));
    }

    public int ResolveMaterial(MaterialTagKey key, Span<MaterialId> span)
    {
        var table = _table[key.Value - 1];
        ref readonly MaterialId r0 = ref table.Slot0;
        var src = MemoryMarshal.CreateReadOnlySpan(ref Unsafe.AsRef(in r0), table.Count);
        src.CopyTo(span);
        return table.Count;

    }

}
/*
public interface IMaterialEntityRegistry
{
    void AttachEntity(EntityId entityId, params MaterialId[] materialIds);
}

public sealed class MaterialEntityRegistry : IMaterialEntityRegistry
{
    private readonly Dictionary<EntityId, MaterialId[]> _register = new(64);
    
    public void AttachEntity(EntityId entityId, params MaterialId[] materialIds)
    {
        ArgumentNullException.ThrowIfNull(materialIds);
        ArgumentOutOfRangeException.ThrowIfLessThanOrEqual(materialIds.Length, 0);
        ArgumentOutOfRangeException.ThrowIfLessThanOrEqual(entityId.Id, 0);
        
        if (!_register.TryAdd(entityId, materialIds))
            throw new InvalidOperationException($"Entity {entityId} already attached to material");
    }

    public ReadOnlySpan<MaterialId> GetMaterialIds(EntityId entityId)
    {
        Debug.Assert(entityId.Id > 0);
        Debug.Assert(_register.ContainsKey(entityId));
        return _register[entityId];
    }
    
}
*/