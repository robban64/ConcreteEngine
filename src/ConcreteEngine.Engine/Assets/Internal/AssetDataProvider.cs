using ConcreteEngine.Engine.Assets.Descriptors;
using ConcreteEngine.Engine.Metadata;

namespace ConcreteEngine.Engine.Assets;

internal interface IAssetData
{
    AssetId AssetId { get; }
    string Name { get; }
    AssetKind Kind { get; }
}

internal sealed class AssetDataProvider
{
    private Dictionary<string, IAssetData> _assetData = new(4);

    public int Count => _assetData.Count;

    public void Add(string name, IAssetData asset) => _assetData.Add(name, asset);
    public bool Remove(string name) => _assetData.Remove(name);
}