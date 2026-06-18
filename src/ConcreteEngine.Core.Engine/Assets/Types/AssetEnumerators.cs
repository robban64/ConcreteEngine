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

public ref struct AssetBindingEnumerator(AssetId assetId, AssetFileRegistry fileRegistry)
{
    private int _i = -1;
    private readonly ReadOnlySpan<AssetFileId> _fileIds = fileRegistry.GetFileBindings(assetId);

    public bool MoveNext() => ++_i < _fileIds.Length;
    public readonly AssetFile Current => fileRegistry.Get(_fileIds[_i]);

    public readonly AssetBindingEnumerator GetEnumerator() => new(assetId, fileRegistry);
}


public ref struct FileBindingEnumerator(ReadOnlySpan<FileBinding> bindings, ReadOnlySpan<AssetFile?> entries)
{
    private int _i = -1;
    private readonly ReadOnlySpan<FileBinding> _bindings = bindings;
    private readonly ReadOnlySpan<AssetFile?> _entries = entries;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool MoveNext() => ++_i < _bindings.Length && _bindings[_i] != FileBinding.Unknown;

    public readonly (AssetFile file, FileBinding binding) Current => (_entries[_i]!, _bindings[_i]);

    public readonly FileBindingEnumerator GetEnumerator() => new(_bindings, _entries);
}