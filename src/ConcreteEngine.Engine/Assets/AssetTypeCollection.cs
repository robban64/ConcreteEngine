using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using ConcreteEngine.Core.Engine.Assets;
using ConcreteEngine.Core.Engine.Assets.Data;
using ConcreteEngine.Engine.Assets.Descriptors;
using ConcreteEngine.Engine.Assets.Utils;

namespace ConcreteEngine.Engine.Assets;
/*
public sealed class AssetCollection
{
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public AssetTypeCollection<T> GetAssetList<T>() where T : AssetObject =>
        (AssetTypeCollection<T>)_collections[AssetKindUtils.ToAssetIndex(typeof(T))];

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public AssetTypeCollection GetAssetList(AssetKind kind) => _collections[kind.ToIndex()];

    private readonly AssetTypeCollection[] _collections;

    public readonly AssetTypeCollection<Shader> Shaders;
    public readonly AssetTypeCollection<Model> Models;
    public readonly AssetTypeCollection<Texture> Textures;
    public readonly AssetTypeCollection<Material> Materials;

    public AssetCollection()
    {
        Shaders = new AssetTypeCollection<Shader>();
        Models = new AssetTypeCollection<Model>();
        Textures = new AssetTypeCollection<Texture>();
        Materials = new AssetTypeCollection<Material>();
        _collections = [Shaders, Models, Textures, Materials];
    }

    internal static AssetTypeCollection[] CreateAll()
    {
        var collections = new AssetTypeCollection[4];
        AssetTypeCollection<Shader>.Create(collections);
        AssetTypeCollection<Model>.Create(collections);
        AssetTypeCollection<Texture>.Create(collections);
        AssetTypeCollection<Material>.Create(collections);
        return collections;
    }
    internal void EnsureStoreCapacity(Queue<AssetRecord>[] queues)
    {
        Shaders.EnsureCapacity(queues[AssetKind.Shader.ToIndex()].Count);
        Models.EnsureCapacity(queues[AssetKind.Model.ToIndex()].Count);
        Textures.EnsureCapacity(queues[AssetKind.Texture.ToIndex()].Count);
        Materials.EnsureCapacity(queues[AssetKind.Material.ToIndex()].Count);
    }

}*/
/*
public abstract class AssetTypeCollection
{
    protected readonly List<AssetFileSpec> Files = [];

    internal readonly HashSet<int> DirtyIds = new(32);

    public abstract int Count { get; }
    public abstract AssetKind Kind { get; }
    public int FileCount => Files.Count;

    public abstract ReadOnlySpan<AssetObject> GetAssets();

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public ReadOnlySpan<AssetFileSpec> GetFileSpan() => CollectionsMarshal.AsSpan(Files);

    public AssetsMetaInfo ToSnapshot() => new(Count, FileCount, Kind);

    public void AddFile(AssetFileSpec fileSpec) => Files.Add(fileSpec);

    internal abstract void Sort();
    internal void MarkDirty(AssetId id) => DirtyIds.Add(id);
    internal void ClearDirty() => DirtyIds.Clear();

}*/

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