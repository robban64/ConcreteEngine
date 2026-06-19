using System.Diagnostics.CodeAnalysis;
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
    private readonly AssetTypeStore[] _storeCollection = new AssetTypeStore[AssetKindUtils.AssetTypeCount];
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

    
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool Has(AssetId id)
    {
        var index = id.Index();
        return (uint)index < (uint)_assets.Length && _assets[index]?.Id == id;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal T GetUnsafe<T>(int id) where T : AssetObject => (T)_assets[id - 1]!;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public T Get<T>(AssetId id) where T : AssetObject
    {
        if (_assets[id.Index()] is T tAsset && tAsset.Id == id) return tAsset;
        Throwers.InvalidOperation("Invalid asset type");
        return null;
    }

    public T GetByName<T>(string name) where T : AssetObject
    {
        if (TryGetByName<T>(name, out var value)) return value;
        Throwers.KeyNotFound(name);
        return null;
    }
    
    public T GetByGuid<T>(Guid gid) where T : AssetObject
    {
        if (TryGetByGuid<T>(gid, out var value)) return value;
        Throwers.KeyNotFound(gid);
        return null;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool TryGet<T>(AssetId id, [NotNullWhen(true)] out T? asset) where T : AssetObject
    {
        var index = id.Index();
        if ((uint)index >= (uint)_assets.Length || _assets[index] is not T tAsset || tAsset.Id != id)
        {
            asset = null;
            return false;
        }
        asset = tAsset;
        return true;
    }

    public bool TryGetByName<T>(string name, [NotNullWhen(true)] out T? asset) where T : AssetObject
    {
        if (!TypeStore<T>.Store.TryGetByName(name, out var assetId))
        {
            asset = null;
            return false;
        }
        return TryGet(assetId, out asset);
    }

    public bool TryGetByGuid<T>(Guid gid, [NotNullWhen(true)] out T? asset) where T : AssetObject
    {
        asset = !_byGid.TryGetValue(gid, out var assetId) || !TryGet<T>(assetId, out var res) ? null : res;
        return asset != null;
    }
    
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool TryGetIdByGuid(Guid gid, out AssetId id) => _byGid.TryGetValue(gid, out id);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public AssetEnumerator GetAssetEnumerator(AssetKind kind) => new(GetTypeStore(kind).AsSpan(), _assets.AsSpan());

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public AssetEnumerator<T> GetAssetEnumerator<T>() where T : AssetObject =>
        new(TypeStore<T>.Store.AsSpan(), _assets.AsSpan());

    [MethodImpl(MethodImplOptions.NoInlining)]
    internal void EnsureStoreCapacity(Queue<AssetRecord>[] queues)
    {
        GetTypeStore(AssetKind.Shader).EnsureCapacity(queues[AssetKind.Shader.ToIndex()].Count);
        GetTypeStore(AssetKind.Model).EnsureCapacity(queues[AssetKind.Model.ToIndex()].Count);
        GetTypeStore(AssetKind.Texture).EnsureCapacity(queues[AssetKind.Texture.ToIndex()].Count);
        GetTypeStore(AssetKind.Material).EnsureCapacity(queues[AssetKind.Material.ToIndex()].Count);
    }
}