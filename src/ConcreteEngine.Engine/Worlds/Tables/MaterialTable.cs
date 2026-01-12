using System.Runtime.CompilerServices;
using ConcreteEngine.Core.Common.Collections;
using ConcreteEngine.Core.Renderer;
using ConcreteEngine.Core.Renderer.Material;
using ConcreteEngine.Engine.Worlds.Data;

namespace ConcreteEngine.Engine.Worlds.Tables;

public sealed class MaterialTable
{
    private static MaterialTagKey CreateTagKey() => new(++_keyIdx);

    private static int _keyIdx;

    public TextureSlotInfo[] CacheSlots { get; set; } = [];

    private MaterialTag[] _table = new MaterialTag[64];
    private readonly Dictionary<MaterialTag, MaterialTagKey> _byTag = new(64);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public MaterialTag GetMaterialTag(MaterialTagKey key) => _table[key.Index()];

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

    public bool TryResolveSubmitMaterial(MaterialTagKey key, out MaterialTag tag)
    {
        var index = key.Index();
        if ((uint)index >= _table.Length)
        {
            tag = default;
            return false;
        }

        tag = _table[index];
        return true;
    }

    public int DrainMaterials(MaterialTagKey key, Span<MaterialId> span)
    {
        var index = key.Index();
        if ((uint)index >= _table.Length)
            throw new IndexOutOfRangeException();

        var table = _table[index];
        var src = table.AsReadOnlySpan();
        src.CopyTo(span);
        return table.EndIndex;
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
}