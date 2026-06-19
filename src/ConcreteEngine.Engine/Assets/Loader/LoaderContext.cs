using ConcreteEngine.Core.Engine.Assets;

namespace ConcreteEngine.Engine.Assets.Loader;

internal readonly ref struct LoaderContext(AssetId id, AssetManager assetManager, ReadOnlySpan<char> filePath)
{
    public readonly AssetId Id = id;
    public readonly ReadOnlySpan<char> FilePath = filePath;

    public AssetFile GetFile(int fileIndex)
    {
        var fileId = assetManager.Store.GetAssetBinding(Id, fileIndex);
        return assetManager.Files.Get(fileId);
    }
}