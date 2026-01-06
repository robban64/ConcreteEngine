namespace ConcreteEngine.Engine.Metadata.Asset;

public interface IAssetObject
{
    AssetId Id { get; }
    Guid GId { get; }

    string Name { get; }
    
    int Generation { get; }
    
    bool IsCoreAsset { get; }
    bool IsEmbedded { get; }

    AssetCategory Category { get; }

    AssetKind Kind { get; }

}