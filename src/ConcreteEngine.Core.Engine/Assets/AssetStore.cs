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

    private AssetObject?[] _assets = new AssetObject?[DefaultCap];
    private AssetFileId[]?[] _bindings = new AssetFileId[DefaultCap][];

    private readonly Dictionary<Guid, AssetId> _byGid = new(DefaultCap);

    private readonly AssetTypeStore[] _storeCollection = new AssetTypeStore[AssetKindUtils.AssetTypeCount];

    private readonly Stack<int> _free = [];

    internal AssetStore() { }

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


    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool HasName(AssetKind kind, string name) => GetTypeStore(kind).HasName(name);

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
        Throwers.NotFoundBy(nameof(T), id);
        return null;
    }

    public T GetByName<T>(string name) where T : AssetObject
    {
        if (TryGetByName<T>(name, out var value)) return value;
         Throwers.NotFoundBy(nameof(T),name);
         return null;
    }

    public T GetByGuid<T>(Guid gid) where T : AssetObject
    {
        if (TryGetByGuid<T>(gid, out var value)) return value;
        Throwers.NotFoundBy(nameof(T), gid);
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

    public bool TryGetIdByGuid(Guid gid, out AssetId id) => _byGid.TryGetValue(gid, out id);

    //
    public bool HasBinding(AssetId id)
    {
        var index = id.Index();
        return (uint)index < (uint)_bindings.Length && _bindings[index] != null;
    }
    
    internal void SetAssetBinding(AssetId assetId, AssetFileId fileId, int fileIndex)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(assetId.Id);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(fileId.Id);

        var fileBinding = _bindings[assetId.Index()]!;
        if (fileBinding[fileIndex].Id > 0)
            Throwers.InvalidArgument($"File {fileIndex}:{fileId} already set for asset {assetId}");

        fileBinding[fileIndex] = fileId;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public AssetFileId GetAssetBinding(AssetId id, int fileIndex)
    {
        var bindings = _bindings[id.Index()];
        if(bindings is null || (uint)fileIndex >= (uint)bindings.Length) Throwers.InvalidArgument(nameof(id));
        var fileId = bindings[fileIndex];
        if (!fileId.IsValid()) Throwers.InvalidArgument(nameof(fileIndex));
        return fileId;
    }
    
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public ReadOnlySpan<AssetFileId> GetAllAssetBindings(AssetId id)
        => _bindings[id.Index()] ?? throw new ArgumentException($"Bindings not found for {id}");

    public bool TryGetFileBindings(AssetId id, out ReadOnlySpan<AssetFileId> bindings)
    {
        var index = id.Index();
        if ((uint)index >= (uint)_bindings.Length || _bindings[index] is not { } fileBinding)
        {
            bindings = default;
            return false;
        }

        bindings = fileBinding;
        return true;
    }
    
    //

    public void AddAsset<TAsset>(TAsset asset) where TAsset : AssetObject
    {
        ArgumentNullException.ThrowIfNull(asset);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(asset.Id.Id);
        ArgumentOutOfRangeException.ThrowIfEqual(asset.GId, Guid.Empty);

        if (Has(asset.Id))
            Throwers.InvalidArgument(nameof(asset), $"Asset '{asset.Name}:{asset.Id}' is already registered.");

        if (TypeStore<TAsset>.Store.HasName(asset.Name))
            asset.Name = AssetNameUtils.IncrementName(asset.Name, typeof(TAsset), NameExistsDel);

        _assets[asset.Id.Index()] = asset;
        TypeStore<TAsset>.Store.Add(asset);
        MarkDirty(asset);
    }

    internal AssetId Register(Guid gid, int fileCount)
    {
        ArgumentOutOfRangeException.ThrowIfEqual(gid, Guid.Empty);
        ArgumentOutOfRangeException.ThrowIfNegative(fileCount);
        ArgumentOutOfRangeException.ThrowIfGreaterThan(fileCount, 16);

        var assetId = AllocateSlot(gid);
        _bindings[assetId.Index()] = new AssetFileId[fileCount + 1];
        return assetId;
    }
    private AssetId AllocateSlot(Guid gid)
    {
        var freeIndex = SlotHelper.NextSlot(_free, Count);
        if (freeIndex >= 0) return new AssetId(freeIndex + 1, 1);

        if (SlotHelper.EnsureCapacity(ref _assets, Count, 1, out var oldSize))
        {
            Array.Resize(ref _bindings, _assets.Length);
            Logger.Log(StringLogEvent.MakeResize(LogScope.Assets, nameof(AssetStore), oldSize, _assets.Length));
        }

        var assetId = new AssetId(++Count, 1);
        _byGid.Add(gid, assetId);
        return assetId;
    }
    
    //

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public AssetEnumerator GetAssetEnumerator(AssetKind kind) => new(GetTypeStore(kind).AsSpan(), _assets.AsSpan());

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public AssetEnumerator<T> GetAssetEnumerator<T>() where T : AssetObject =>
        new(TypeStore<T>.Store.AsSpan(), _assets.AsSpan());


}