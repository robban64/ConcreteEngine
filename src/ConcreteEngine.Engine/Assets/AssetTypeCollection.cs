using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using ConcreteEngine.Core.Engine.Assets;
using ConcreteEngine.Core.Engine.Assets.Data;
using ConcreteEngine.Engine.Assets.Utils;

namespace ConcreteEngine.Engine.Assets;

internal sealed class AssetTypeCollection(AssetKind kind)
{
    private readonly List<AssetId> _asset = [];
    private readonly HashSet<int> _dirtyIds = [];

    public AssetKind Kind { get; } = kind;
    public int Count => _asset.Count;
    public int DirtyCount => _dirtyIds.Count;

    public AssetsMetaInfo ToSnapshot() => new(Count, 0, Kind);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public ReadOnlySpan<AssetId> AsSpan() => CollectionsMarshal.AsSpan(_asset);

    public void Add(AssetObject asset)
    {
        ArgumentOutOfRangeException.ThrowIfNotEqual((int)asset.Kind, (int)Kind, nameof(asset));
        _asset.Add(asset.Id);
    }

    public void MarkDirty(AssetObject asset)
    {
        ArgumentOutOfRangeException.ThrowIfNotEqual((int)asset.Kind, (int)Kind, nameof(asset));
        _dirtyIds.Add(asset.Id);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void ClearDirty() => _dirtyIds.Clear();

    public void Sort() => _asset.Sort();

    public void EnsureCapacity(int capacity) => _asset.EnsureCapacity(capacity);

    public HashSet<int>.Enumerator GetDirtyEnumerator() => _dirtyIds.GetEnumerator();

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