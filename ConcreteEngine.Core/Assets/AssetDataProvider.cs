using ConcreteEngine.Core.Assets.Descriptors;

namespace ConcreteEngine.Core.Assets;

internal sealed class AssetDataProvider
{
    private Dictionary<string, IAssetData> _assetData = new(4);
    
    public int Count => _assetData.Count;

    public void Add(string name, IAssetData asset) => _assetData.Add(name, asset);
    public bool Remove(string name) => _assetData.Remove(name);
}


