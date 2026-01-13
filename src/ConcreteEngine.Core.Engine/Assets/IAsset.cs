namespace ConcreteEngine.Core.Engine.Assets;

public interface IAsset
{
    AssetId Id { get; }
    Guid GId { get; }
    string Name { get; }
    bool IsCoreAsset { get; }
    int Generation { get; }

    AssetCategory Category { get; }
    AssetKind Kind { get; }
}