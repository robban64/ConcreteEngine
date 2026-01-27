using ConcreteEngine.Core.Common.Text;
using ConcreteEngine.Core.Engine.Assets;

namespace ConcreteEngine.Engine.Assets;

public abstract record AssetObject : IAsset, IComparable<AssetObject>
{
    public required AssetId Id { get; init; }
    public required Guid GId { get; init; } = Guid.NewGuid();

    public required string Name
    {
        get;
        init
        {
            if (field == value) return;
            field = value;
            PackedName = StringPacker.Pack(value.AsSpan());
        }
    }
    internal ulong PackedName { get; private set; }

    public bool IsCoreAsset { get; init; }
    public int Generation { get; init; } = 1;
    
    public abstract AssetCategory Category { get; }
    public abstract AssetKind Kind { get; }

    public int CompareTo(AssetObject? other)
    {
        return other is null ? 1 : Id.Value.CompareTo(other.Id.Value);
    }
}