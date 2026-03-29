using System.Runtime.CompilerServices;
using ConcreteEngine.Core.Engine.Assets;
using ConcreteEngine.Core.Engine.Assets.Data;
using ConcreteEngine.Engine.Assets.Utils;

namespace ConcreteEngine.Engine.Assets;

public sealed partial class AssetStore
{
    public AssetsMetaInfo GetMetaSnapshot<TAsset>() where TAsset : AssetObject => GetAssetList<TAsset>().ToSnapshot();

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public AssetTypeCollection<T> GetAssetList<T>() where T : AssetObject =>
        (AssetTypeCollection<T>)_collections[AssetKindUtils.ToAssetIndex(typeof(T))];

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public AssetTypeCollection GetAssetList(AssetKind kind) => _collections[kind.ToIndex()];

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public AssetObject Get(AssetId assetId) => _assets[assetId.Index()];

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public T Get<T>(AssetId assetId) where T : AssetObject
    {
        var asset = _assets[assetId.Index()];
        if (asset is T tAsset) return tAsset;
        throw new KeyNotFoundException($"Asset '{assetId.Value}' not found or incorrect type.");
    }

    public T GetByName<T>(string name) where T : AssetObject
    {
        if (TryGetByName<T>(name, out var value)) return value!;
        throw new KeyNotFoundException($"Asset GetByName '{name}' not found or incorrect type.");
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool TryGet(AssetId assetId, out AssetObject asset)
    {
        var index = assetId.Index();
        asset = (uint)index >= (uint)_assets.Capacity ? null! : _assets[index];
        return asset != null!;
    }

    public bool TryGet<T>(AssetId assetId, out T asset) where T : AssetObject
    {
        asset = !TryGet(assetId, out var res) || res is not T tRes ? null! : tRes;
        return asset != null!;
    }

    public bool TryGetByName<T>(string name, out T asset) where T : AssetObject
    {
        asset = !TryGetByName(name, typeof(T), out var res) || res is not T tRes ? null! : tRes;
        return asset != null!;
    }

    public bool TryGetByName(string name, Type type, out AssetObject asset)
    {
        asset = !_byName.TryGetValue((type, name), out var id) || !TryGet(id, out var objT) ? null! : objT;
        return asset != null!;
    }

    public bool TryGetByGuid<T>(Guid guid, out T asset) where T : AssetObject
    {
        asset = !TryGetByGuid(guid, out var res) || res is not T tRes ? null! : tRes;
        return asset != null!;
    }

    public bool TryGetByGuid(Guid gid, out AssetObject asset)
    {
        asset = !_byGid.TryGetValue(gid, out var assetId) || !TryGet(assetId, out var res) ? null! : res;
        return asset != null!;
    }
    
    public bool TryGetIdByGuid(Guid gid, out AssetId assetId) => _byGid.TryGetValue(gid, out assetId);
}