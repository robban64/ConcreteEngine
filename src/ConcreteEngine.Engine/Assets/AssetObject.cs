using ConcreteEngine.Engine.Metadata;

namespace ConcreteEngine.Engine.Assets;

public abstract class AssetObject : IComparable<AssetObject>
{
    internal AssetId RawId { get; init; }
    public string Name { get; internal set; } = null!;
    public required bool IsCoreAsset { get; init; }
    public int Generation { get; private set; }

    public bool IsEmbedded { get; internal set; } = false;

    public Guid GId
    {
        get => field;
        internal set
        {
            if (field != Guid.Empty) throw new InvalidOperationException("GId already set");
            field = value;
        }
    }


    public abstract AssetKind Kind { get; }
    public abstract AssetCategory Category { get; }


    internal void BumpGeneration() => Generation++;

    public int CompareTo(AssetObject? other)
    {
        return other is null ? 1 : RawId.Value.CompareTo(other.RawId.Value);
    }
}