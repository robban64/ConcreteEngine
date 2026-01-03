using ConcreteEngine.Engine.Metadata;

namespace ConcreteEngine.Engine.Assets;

public abstract class AssetObject : IComparable<AssetObject>
{
    public AssetId Id { get; init; }
    public string Name { get; internal set; }
    public bool IsCoreAsset { get; internal init; }
    public int Generation { get; private set; } = 1;
    public bool IsEmbedded { get; internal set; }

    protected AssetObject()
    {
    }

    public Guid GId
    {
        get => field;
        internal set
        {
            if (field != Guid.Empty) throw new InvalidOperationException("GId already set");
            field = value;
        }
    }

    public abstract AssetCategory Category { get; }
    public abstract AssetKind Kind { get; }

    internal void BumpGeneration() => Generation++;

    public int CompareTo(AssetObject? other)
    {
        return other is null ? 1 : Id.Value.CompareTo(other.Id.Value);
    }
}