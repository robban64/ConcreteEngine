using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using ConcreteEngine.Core.Common.Collections;
using ConcreteEngine.Core.Engine.Assets.Data;
using ConcreteEngine.Core.Engine.Assets.Utils;

namespace ConcreteEngine.Core.Engine.Assets;

public sealed class AssetTypeCollection(AssetKind kind)
{
    private readonly List<AssetId> _asset = [];
    private readonly List<int> _dirtyIds = [];

    public AssetKind Kind { get; } = kind;
    public int Count => _asset.Count;
    public int DirtyCount => _dirtyIds.Count;

    public AssetsMetaInfo ToSnapshot() => new(Count, 0, Kind);
    
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public ReadOnlySpan<int> GetDirtySpan() => CollectionsMarshal.AsSpan(_dirtyIds);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public ReadOnlySpan<AssetId> AsSpan() => CollectionsMarshal.AsSpan(_asset);

    public void Add(AssetObject asset)
    {
        ArgumentOutOfRangeException.ThrowIfNotEqual((int)asset.Kind, (int)Kind, nameof(asset));
        _asset.Add(asset.Id);
    }

    public void MarkDirty(AssetObject asset)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(asset.Id.Value, nameof(asset.Id));
        ArgumentOutOfRangeException.ThrowIfNotEqual((int)asset.Kind, (int)Kind, nameof(asset));

        var id = asset.Id.Value;
        if (_dirtyIds.Count == 0)
        {
            _dirtyIds.Add(id);
            return;
        }

        var lastId = _dirtyIds[^1];
        if(lastId == id) return;

        if (id > lastId)
        {
            _dirtyIds.Add(id);
            return;
        }

        var existingIndex = SearchMethod.BinarySearch(CollectionsMarshal.AsSpan(_dirtyIds), id);
        if(existingIndex >= 0) return;
        _dirtyIds.Add(id);
        _dirtyIds.Sort();
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void ClearDirty() => _dirtyIds.Clear();

    public void Sort() => _asset.Sort();

    public void EnsureCapacity(int capacity)
    {
        _asset.EnsureCapacity(capacity);
        _dirtyIds.EnsureCapacity(capacity);
    }


    [MethodImpl(MethodImplOptions.NoInlining)]
    internal static AssetTypeCollection[] CreateAll()
    {
        var collections = new AssetTypeCollection[4];
        collections[AssetKind.Shader.ToIndex()] = new AssetTypeCollection(AssetKind.Shader);
        collections[AssetKind.Model.ToIndex()] = new AssetTypeCollection(AssetKind.Model);
        collections[AssetKind.Texture.ToIndex()] = new AssetTypeCollection(AssetKind.Texture);
        collections[AssetKind.Material.ToIndex()] = new AssetTypeCollection(AssetKind.Material);
        return collections;
    }
}