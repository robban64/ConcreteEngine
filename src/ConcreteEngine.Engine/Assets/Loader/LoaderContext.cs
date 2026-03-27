using ConcreteEngine.Core.Engine.Assets;

namespace ConcreteEngine.Engine.Assets.Loader;

internal readonly ref struct LoaderContext(AssetId id, AssetStore store)
{
    public readonly AssetId Id = id;
    public AssetFileSpec GetFile(int fileIndex)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(fileIndex);
        var fileIds = store.GetFileIds(Id);
        ArgumentOutOfRangeException.ThrowIfGreaterThanOrEqual(fileIndex, fileIds.Length);
        if (!store.TryGetFileEntry(fileIds[fileIndex], out var result))
            throw new InvalidOperationException($"Missing file for AssetFileId: {fileIds[fileIndex]}");
        return result;
    }
    
    public readonly LoaderContextEnumerator GetEnumerator() => new(Id, store);

    internal ref struct LoaderContextEnumerator(AssetId assetId, AssetStore store)
    {
        private readonly ReadOnlySpan<AssetFileId> _fileIds = store.GetFileIds(assetId);
        private int _i = -1;

        public bool MoveNext() => ++_i < _fileIds.Length;

        public readonly AssetFileSpec Current
        {
            get
            {
                store.TryGetFileEntry(_fileIds[_i], out var fileSpec);
                return fileSpec;
            }
        }

    }
}