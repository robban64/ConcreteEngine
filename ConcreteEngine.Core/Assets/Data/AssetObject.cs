namespace ConcreteEngine.Core.Assets.Data;

public abstract class AssetObject
{
    internal AssetId RawId { get; init; }
    public required string Name { get; init; }
    public required bool IsCoreAsset { get; init; }
    public int Generation { get; private set; }

    public abstract AssetKind Kind { get; }
    public abstract AssetCategory Category { get; }
    
    internal int ResourceId {get; init;}

    protected void BumpGeneration() => Generation++;

}