using ConcreteEngine.Core.Common;

namespace ConcreteEngine.Core.Engine.Assets;

public readonly record struct AssetId(int Value) : IComparable<AssetId>
{
    public bool IsValid() => Value > 0;
    public int Index() => Value - 1;

    public int CompareTo(AssetId other) => Value.CompareTo(other.Value);

    public static implicit operator Id32<AssetObject>(AssetId id) => new(id.Value);
    public static explicit operator AssetId(Id32<AssetObject> id) => new(id.Value);

    public static readonly AssetId Empty = new(0);
}

public readonly record struct AssetIndexRef(Guid AssetGId, int Index);