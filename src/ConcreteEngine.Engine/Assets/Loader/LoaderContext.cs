using ConcreteEngine.Core.Engine.Assets;

namespace ConcreteEngine.Engine.Assets.Loader;

internal readonly ref struct LoaderContext(AssetId id, AssetStore store)
{
    public readonly AssetId Id = id;

    public AssetFileSpec GetFile(int fileIndex)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(fileIndex);
        var fileIds = store.FileRegistry.GetFileBindings(Id);
        ArgumentOutOfRangeException.ThrowIfGreaterThanOrEqual(fileIndex, fileIds.Length);
        if (!store.FileRegistry.TryGetFile(fileIds[fileIndex], out var result))
            throw new InvalidOperationException($"Missing file for AssetFileId: {fileIds[fileIndex]}");

        return result;
    }
}