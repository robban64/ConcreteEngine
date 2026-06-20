using ConcreteEngine.Core.Engine.Assets;

namespace ConcreteEngine.Engine.Assets.Loader;

internal readonly ref struct ImportContext(AssetId id, AssetManager assetManager)
{
    public readonly AssetId Id = id;

    public AssetFile GetRootFile() => assetManager.GetAssetRootFile(Id);
    public AssetFile GetFile(int fileIndex) => assetManager.GetAssetFile(Id, fileIndex);
}