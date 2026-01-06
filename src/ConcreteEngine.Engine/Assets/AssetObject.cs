using ConcreteEngine.Engine.Metadata;
using ConcreteEngine.Engine.Metadata.Asset;

namespace ConcreteEngine.Engine.Assets;

public abstract class AssetObject : IAssetObject, IComparable<AssetObject>
{
    public required AssetId Id { get;  init;}
    public  Guid GId { get; init; } = Guid.NewGuid();
    public string Name { get; internal set; }
    public bool IsCoreAsset { get; init; }
    public bool IsEmbedded { get; internal set; }
    public int Generation { get; private set; } = 1;

    public abstract AssetCategory Category { get; }
    public abstract AssetKind Kind { get; }

    protected AssetObject()
    {
    }

    internal void BumpGeneration() => Generation++;

    public int CompareTo(AssetObject? other)
    {
        return other is null ? 1 : Id.Value.CompareTo(other.Id.Value);
    }
}