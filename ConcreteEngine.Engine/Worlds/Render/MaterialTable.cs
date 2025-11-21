#region

using System.Runtime.CompilerServices;
using ConcreteEngine.Common.Collections;
using ConcreteEngine.Engine.Worlds.Data;
using ConcreteEngine.Renderer.Data;

#endregion

namespace ConcreteEngine.Engine.Worlds.Render;

public interface IMaterialTable
{
    MaterialTagKey Add(in MaterialTag tag);
}

public sealed class MaterialTable : IMaterialTable
{
    private int _keyIdx = 0;
    private MaterialTag[] _table = new MaterialTag[64];
    private readonly Dictionary<MaterialTag, MaterialTagKey> _byTag = new(64);


    public void PushTemporary(in MaterialTag tag)
    {
    }
    

    public MaterialTagKey Add(in MaterialTag tag)
    {
        ArgumentOutOfRangeException.ThrowIfLessThanOrEqual(tag.Slot0, 0);
        EnsureCapacity(1);
        return AddTag(tag);
    }

/*
    public MaterialTagKey AddSpan(ReadOnlySpan<MaterialId> s)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(s.Length, 8, nameof(s));
        EnsureCapacity(1);
        var tag = new MaterialTag(s[0], s[1], s[2], s[3], s[4], s[5]);
        return AddTag(tag);
    }
*/
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

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void ResolveSubmitMaterial(MaterialTagKey key, out MaterialTag tag) => tag = _table[key.Value - 1];

    public int DrainMaterials(MaterialTagKey key, Span<MaterialId> span)
    {
        var table = _table[key.Value - 1];
        var src = table.AsReadOnlySpan();
        src.CopyTo(span);
        return table.EndIndex;
    }
}