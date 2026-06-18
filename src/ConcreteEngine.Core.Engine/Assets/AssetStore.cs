using System.Runtime.CompilerServices;
using ConcreteEngine.Core.Common;
using ConcreteEngine.Core.Common.Collections;
using ConcreteEngine.Core.Diagnostics.Logging;
using ConcreteEngine.Core.Engine.Assets.Descriptors;
using ConcreteEngine.Core.Engine.Assets.Utils;
using ConcreteEngine.Renderer.Buffer;
using ConcreteEngine.Renderer.Core;

namespace ConcreteEngine.Core.Engine.Assets;


public sealed partial class AssetStore
{
    private const int DefaultCap = 512;

    public int Count { get; private set; }

    public readonly AssetFileRegistry FileRegistry;

    private AssetObject?[] _assets = new AssetObject?[DefaultCap];
    private readonly AssetTypeStore[] _storeCollection = new AssetTypeStore[StoreCount];
    private readonly Dictionary<Guid, AssetId> _byGid = new(DefaultCap);

    private readonly Stack<int> _free = [];

    internal AssetStore(AssetFileRegistry fileRegistry) => FileRegistry = fileRegistry;

    internal void SetupStores()
    {
        _storeCollection[AssetKind.Shader.ToIndex()] = TypeStore<Shader>.Store;
        _storeCollection[AssetKind.Model.ToIndex()] = TypeStore<Model>.Store;
        _storeCollection[AssetKind.Texture.ToIndex()] = TypeStore<Texture>.Store;
        _storeCollection[AssetKind.Material.ToIndex()] = TypeStore<Material>.Store;
    }

    //
    public int FreeCount => _free.Count;
    public int ActiveCount => Count - _free.Count;
    public int Capacity => _assets.Length;
    internal ReadOnlySpan<AssetTypeStore> GetTypeStoreSpan() => _storeCollection;
    //

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public AssetTypeStore GetTypeStore(AssetKind kind) => _storeCollection[kind.ToIndex()];

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public AssetTypeStore GetTypeStore<T>() where T : AssetObject => TypeStore<T>.Store;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal void MarkDirty(AssetObject asset) => GetTypeStore(asset.Kind).MarkDirty(asset);

    internal void Rename(AssetObject asset, string newName)
    {
        AssetNameUtils.ValidateAssetName(newName);
        if (asset.Name == newName)
            throw new ArgumentException("Rename: Identical name", nameof(newName));

        GetTypeStore(asset.Kind).Rename(asset.Name, newName);
    }

    internal AssetId AllocateSlot(Guid gid)
    {
        var freeIndex = SlotHelper.NextSlot(_free, Count);
        if (freeIndex >= 0) return new AssetId(freeIndex + 1, 1);

        if (SlotHelper.EnsureCapacity(ref _assets, Count, 1, out var oldSize))
            Logger.Log(StringLogEvent.MakeResize(LogScope.Assets, nameof(AssetStore), oldSize, _assets.Length));

        var assetId = new AssetId(++Count, 1);
        _byGid.Add(gid, assetId);
        return assetId;
    }

    public void AddAsset<TAsset>(TAsset asset) where TAsset : AssetObject
    {
        ArgumentNullException.ThrowIfNull(asset);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(asset.Id.Value);
        ArgumentOutOfRangeException.ThrowIfEqual(asset.GId, Guid.Empty);

        if (Has(asset.Id))
            throw new InvalidOperationException($"Asset '{asset.Name}:{asset.Id}' is already registered.");

        if (!FileRegistry.TryGetFileBindings(asset.Id, out _))
            throw new InvalidOperationException($"Asset '{asset.Name}:{asset.Id}' missing file bindings.");

        var assetList = GetTypeStore(asset.Kind);

        if (assetList.HasName(asset.Name))
            asset.Name = AssetNameUtils.IncrementName(asset.Name, typeof(TAsset), NameExistsDel);

        _assets[asset.Id.Index()] = asset;
        assetList.Add(asset);
        MarkDirty(asset);
    }


    [MethodImpl(MethodImplOptions.NoInlining)]
    internal void EnsureStoreCapacity(Queue<AssetRecord>[] queues)
    {
        GetTypeStore(AssetKind.Shader).EnsureCapacity(queues[AssetKind.Shader.ToIndex()].Count);
        GetTypeStore(AssetKind.Model).EnsureCapacity(queues[AssetKind.Model.ToIndex()].Count);
        GetTypeStore(AssetKind.Texture).EnsureCapacity(queues[AssetKind.Texture.ToIndex()].Count);
        GetTypeStore(AssetKind.Material).EnsureCapacity(queues[AssetKind.Material.ToIndex()].Count);
    }
}