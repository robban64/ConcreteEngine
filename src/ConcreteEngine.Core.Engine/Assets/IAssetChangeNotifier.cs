namespace ConcreteEngine.Core.Engine.Assets;

public interface IAssetChangeNotifier
{
    void MarkDirty(AssetObject asset);
}