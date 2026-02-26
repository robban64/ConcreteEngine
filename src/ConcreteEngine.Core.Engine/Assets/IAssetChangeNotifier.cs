namespace ConcreteEngine.Core.Engine.Assets;

public interface IAssetChangeNotifier
{
    void MarkDirty(AssetObject asset);
    void Rename(AssetObject asset, string newName, Action<string> onSuccess);
}