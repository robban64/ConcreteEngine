namespace ConcreteEngine.Engine.Assets;

public abstract class AssetObject : IComparable<AssetObject>
{
    internal AssetId RawId { get; init; }
    public string Name { get; internal set; } = null!;
    public required bool IsCoreAsset { get; init; }
    public int Generation { get; private set; }

    public bool IsEmbedded { get; internal set; } = false;

    public abstract AssetKind Kind { get; }
    public abstract AssetCategory Category { get; }


    internal void BumpGeneration() => Generation++;

    public int CompareTo(AssetObject? other)
    {
        return other is null ? 1 : RawId.Value.CompareTo(other.RawId.Value);
    }
}