namespace ConcreteEngine.Core.Assets.Data;

public interface IAssetDescriptor
{
    string Name { get; }
    AssetKind Kind { get; }
}

public interface IAssetCatalog
{
    int Count { get; }
}