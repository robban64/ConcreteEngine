namespace ConcreteEngine.Core.Assets.Data;

public abstract class AssetObject
{
    internal AssetId RawId { get; init; }
    public required string Name { get; init; }
    public required bool IsCoreAsset { get; init; }
    public required int Generation { get; init; }

    public abstract AssetKind Kind { get; }
    public abstract AssetCategory Category { get; }
}