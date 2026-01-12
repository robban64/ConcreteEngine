namespace ConcreteEngine.Core.Engine.Assets;

public abstract record AssetObject : IComparable<AssetObject>
{
    public required AssetId Id { get; init; }
    public Guid GId { get; init; } = Guid.NewGuid();
    public string Name { get; init; }
    public bool IsCoreAsset { get; init; }
    public bool IsEmbedded { get; init; }
    public int Generation { get; init; } = 1;

    public abstract AssetCategory Category { get; }
    public abstract AssetKind Kind { get; }


    public int CompareTo(AssetObject? other)
    {
        return other is null ? 1 : Id.Value.CompareTo(other.Id.Value);
    }
}