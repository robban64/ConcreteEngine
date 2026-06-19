using ConcreteEngine.Core.Engine.Assets;

namespace ConcreteEngine.Engine.Assets.Loader;

internal readonly ref struct LoaderContext(AssetId id, AssetManager assetManager)
{
    public readonly AssetId Id = id;

    public AssetFile GetFile(int fileIndex)
    {
        var fileIds = assetManager.Store.GetFileBindings(Id);
        ArgumentOutOfRangeException.ThrowIfGreaterThanOrEqual((uint)fileIndex, (uint)fileIds.Length);
        return assetManager.Files.Get(fileIds[fileIndex]);
    }
}