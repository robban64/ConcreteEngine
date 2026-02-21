namespace ConcreteEngine.Core.Engine.Assets;

public interface IAssetChangeNotifier
{
    void MarkDirty(AssetId id);
}