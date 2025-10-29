#region

using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using ConcreteEngine.Common.Collections;
using ConcreteEngine.Core.World.Data;
using ConcreteEngine.Renderer.Data;

#endregion

namespace ConcreteEngine.Core.World.Render;

public interface IMaterialTable
{
    MaterialTagKey Add(MaterialId m0, MaterialId m1 = default, MaterialId m2 = default, MaterialId m3 = default,
        MaterialId m4 = default, MaterialId m5 = default, MaterialId m6 = default, MaterialId m7 = default);

    MaterialTagKey AddSpan(ReadOnlySpan<MaterialId> s);
}

public sealed class MaterialTable : IMaterialTable
{
    private int _keyIdx = 0;
    private MaterialTag[] _table = new MaterialTag[64];
    private readonly Dictionary<MaterialTag, MaterialTagKey> _byTag = new(64);

    public MaterialTagKey Add(MaterialId m0, MaterialId m1 = default, MaterialId m2 = default, MaterialId m3 = default,
        MaterialId m4 = default, MaterialId m5 = default, MaterialId m6 = default, MaterialId m7 = default)
    {
        ArgumentOutOfRangeException.ThrowIfLessThanOrEqual(m0.Id, 0);
        EnsureCapacity(1);
        var tag = new MaterialTag(m0, m1, m2, m3, m4, m5, m6);
        return AddTag(tag);
    }

    public MaterialTagKey AddSpan(ReadOnlySpan<MaterialId> s)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(s.Length, 8, nameof(s));
        EnsureCapacity(1);
        var tag = new MaterialTag(s[0], s[1], s[2], s[3], s[4], s[5], s[6]);
        return AddTag(tag);
    }

    private MaterialTagKey AddTag(MaterialTag tag)
    {
        if (_byTag.TryGetValue(tag, out var key)) return key;

        key = new MaterialTagKey(_keyIdx + 1);
        _byTag[tag] = key;
        _table[_keyIdx++] = tag;
        return key;
    }

    private void EnsureCapacity(int n)
    {
        var newSize = _keyIdx + n;
        if (newSize >= _table.Length)
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