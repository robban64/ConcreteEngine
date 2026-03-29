using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using ConcreteEngine.Core.Engine.Assets;
using ConcreteEngine.Core.Engine.Assets.Data;
using ConcreteEngine.Engine.Assets.Descriptors;
using ConcreteEngine.Engine.Assets.Utils;

namespace ConcreteEngine.Engine.Assets;

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
        _collections = [Shaders,Models,Textures,Materials];
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

}

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

    internal static AssetTypeCollection[] CreateAll()
    {
        var collections = new AssetTypeCollection[4];
        AssetTypeCollection<Shader>.Create(collections);
        AssetTypeCollection<Model>.Create(collections);
        AssetTypeCollection<Texture>.Create(collections);
        AssetTypeCollection<Material>.Create(collections);
        return collections;
    }
}

public sealed class AssetTypeCollection<T>() : AssetTypeCollection where T : AssetObject
{
    private readonly List<T> _asset = [];

    public override int Count => _asset.Count;
    public override AssetKind Kind => AssetKindUtils.ToAssetKind(typeof(T));

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public ReadOnlySpan<T> GetTypedAssets() => CollectionsMarshal.AsSpan(_asset);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public override ReadOnlySpan<AssetObject> GetAssets() => CollectionsMarshal.AsSpan(_asset);

    public void Add(T asset) => _asset.Add(asset);

    internal override void Sort()
    {
        _asset.Sort();
        Files.Sort();
    }

    public void EnsureCapacity(int capacity)
    {
        _asset.EnsureCapacity(capacity);
        Files.EnsureCapacity(capacity);
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    internal static void Create(AssetTypeCollection[] array)
    {
        var kind = AssetKindUtils.ToAssetKind(typeof(T));
        var idx = (int)kind - 1;

        if (array[idx] != null) throw new ArgumentException(nameof(array));
        array[idx] = new AssetTypeCollection<T>();
    }
}