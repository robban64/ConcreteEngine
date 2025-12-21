using System.Runtime.CompilerServices;
using ConcreteEngine.Common.Collections;
using ConcreteEngine.Engine.Worlds.Data;
using ConcreteEngine.Renderer;
using ConcreteEngine.Renderer.Data;

namespace ConcreteEngine.Engine.Worlds.Tables;

public interface IMaterialTable
{
    MaterialTagKey Add(in MaterialTag tag);
}

public sealed class MaterialTable : IMaterialTable
{
    private static MaterialTagKey CreateTagKey() => new(++_keyIdx);

    private static int _keyIdx = 0;
    
    private MaterialTag[] _table = new MaterialTag[64];
    private readonly Dictionary<MaterialTag, MaterialTagKey> _byTag = new(64);
    
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public MaterialTag GetMaterialTag(MaterialTagKey key) => _table[key.Value - 1];

    public MaterialTagKey Add(in MaterialTag tag)
    {
        ArgumentOutOfRangeException.ThrowIfLessThanOrEqual(tag.Slot0, 0);
        EnsureCapacity(1);
        return AddTag(tag);
    }

    private MaterialTagKey AddTag(MaterialTag tag)
    {
        if (_byTag.TryGetValue(tag, out var key)) return key;

        _table[_keyIdx] = tag;

        key = CreateTagKey();
        _byTag[tag] = key;
        return key;
    }

    private void EnsureCapacity(int n)
    {
        var newSize = _keyIdx + n;
        if (_table.Length < newSize)
        {
            Console.WriteLine("Resize material table");
            Array.Resize(ref _table, Arrays.CapacityGrowthLinear(_keyIdx, newSize, 32));
        }
    }

    public bool TryResolveSubmitMaterial(MaterialTagKey key, out MaterialTag tag)
    {
        var index = key.Value - 1;
        if ((uint)index >= _table.Length)
        {
            tag = default;
            return false;
        }

        tag = _table[key.Value - 1];
        return true;
    }

    public int DrainMaterials(MaterialTagKey key, Span<MaterialId> span)
    {
        var table = _table[key.Value - 1];
        var src = table.AsReadOnlySpan();
        src.CopyTo(span);
        return table.EndIndex;
    }
}