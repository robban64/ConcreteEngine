using System.Runtime.CompilerServices;

namespace ConcreteEngine.Core.Engine.Assets;

public ref struct AssetEnumerator(ReadOnlySpan<AssetId> assetIds, ReadOnlySpan<AssetObject?> assets)
{
    private int _i = -1;
    private readonly ReadOnlySpan<AssetId> _assetIds = assetIds;
    private readonly ReadOnlySpan<AssetObject?> _assets = assets;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool MoveNext() => ++_i < _assetIds.Length;

    public readonly AssetObject Current => _assets[_assetIds[_i].Index()]!;

    public readonly AssetEnumerator GetEnumerator() => new(_assetIds, _assets);
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

public ref struct AssetBindingEnumerator(ReadOnlySpan<AssetFileId> fileIds, AssetFileRegistry fileRegistry)
{
    private int _i = -1;
    private readonly ReadOnlySpan<AssetFileId> _fileIds = fileIds;

    public bool MoveNext() => ++_i < _fileIds.Length;
    public readonly AssetFile Current => fileRegistry.Get(_fileIds[_i]);

    public readonly AssetBindingEnumerator GetEnumerator() => new(_fileIds, fileRegistry);
}