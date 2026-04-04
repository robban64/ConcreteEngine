namespace ConcreteEngine.Core.Engine.Assets.Data;

public ref struct AssetFilesEnumerator(AssetId assetId, AssetProvider provider)
{
    private int _i = -1;
    private readonly ReadOnlySpan<AssetFileId> _fileIds = provider.GetAssetFileBindings(assetId);

    public bool MoveNext() => ++_i < _fileIds.Length;
    public readonly AssetFileSpec Current => provider.GetFile(_fileIds[_i]);

    public AssetFilesEnumerator GetEnumerator()
    {
        _i = -1;
        return this;
    }
}


public ref struct AssetEnumerator(ReadOnlySpan<AssetId> assetIds, ReadOnlySpan<AssetObject> assets)
{
    private int _i = -1;
    private readonly ReadOnlySpan<AssetId> _assetIds = assetIds;
    private readonly ReadOnlySpan<AssetObject> _assets = assets;

    public bool MoveNext() => ++_i < _assetIds.Length;
    public readonly AssetObject Current => _assets[_assetIds[_i].Index()];

    public AssetEnumerator GetEnumerator()
    {
        _i = -1;
        return this;
    }
}

public ref struct AssetEnumerator<T>(ReadOnlySpan<AssetId> assetIds, ReadOnlySpan<AssetObject> assets)
    where T : AssetObject
{
    private int _i = -1;
    private readonly ReadOnlySpan<AssetId> _assetIds = assetIds;
    private readonly ReadOnlySpan<AssetObject> _assets = assets;

    public bool MoveNext() => ++_i < _assetIds.Length;
    public readonly T Current => (T)_assets[_assetIds[_i].Index()];

    public AssetEnumerator<T> GetEnumerator()
    {
        _i = -1;
        return this;
    }
}

public ref struct FileSpecEnumerator(ReadOnlySpan<AssetFileId> ids, ReadOnlySpan<AssetFileSpec> entries)
{
    private int _i = -1;
    private readonly ReadOnlySpan<AssetFileId> _ids = ids;
    private readonly ReadOnlySpan<AssetFileSpec> _entries = entries;

    public bool MoveNext() => ++_i < _ids.Length;
    public readonly AssetFileSpec Current => _entries[_ids[_i].Index()];

    public FileSpecEnumerator GetEnumerator()
    {
        _i = -1;
        return this;
    }
}