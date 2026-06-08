using System.Runtime.CompilerServices;

namespace ConcreteEngine.Core.Engine.Assets;

public ref struct AssetFilesEnumerator(AssetId assetId, AssetFileRegistry fileRegistry)
{
    private int _i = -1;
    private readonly ReadOnlySpan<AssetFileId> _fileIds = fileRegistry.GetFileBindings(assetId);

    public bool MoveNext() => ++_i < _fileIds.Length;
    public readonly AssetFile Current => fileRegistry.Get(_fileIds[_i]);

    public readonly AssetFilesEnumerator GetEnumerator() => new(assetId, fileRegistry);
}

public ref struct AssetEnumerator(ReadOnlySpan<AssetId> assetIds, ReadOnlySpan<AssetObject?> assets)
{
    private int _i = -1;
    private readonly ReadOnlySpan<AssetId> _assetIds = assetIds;
    private readonly ReadOnlySpan<AssetObject?> _assets = assets;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool MoveNext() => ++_i < _assetIds.Length;

    public readonly AssetObject Current => _assets[_assetIds[_i].Index()]!;

    public readonly AssetEnumerator GetEnumerator() => new (_assetIds, _assets);
}

public ref struct AssetEnumerator<T>(ReadOnlySpan<AssetId> assetIds, ReadOnlySpan<AssetObject?> assets)
    where T : AssetObject
{
    private int _i = -1;
    private readonly ReadOnlySpan<AssetId> _assetIds = assetIds;
    private readonly ReadOnlySpan<AssetObject?> _assets = assets;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool MoveNext() => ++_i < _assetIds.Length;

    public readonly T Current => (T)_assets[_assetIds[_i].Index()]!;

    public readonly AssetEnumerator<T> GetEnumerator() => new(_assetIds, _assets);
}

public ref struct FileSpecEnumerator(ReadOnlySpan<AssetFileId> ids, ReadOnlySpan<AssetFile?> entries)
{
    private int _i = -1;
    private readonly ReadOnlySpan<AssetFileId> _ids = ids;
    private readonly ReadOnlySpan<AssetFile?> _entries = entries;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool MoveNext() => ++_i < _ids.Length;

    public readonly AssetFile Current => _entries[_ids[_i].Index()]!;

    public readonly FileSpecEnumerator GetEnumerator() => new(_ids, _entries);
}