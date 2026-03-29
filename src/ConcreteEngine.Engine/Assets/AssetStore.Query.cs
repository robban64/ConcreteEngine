using System.Runtime.CompilerServices;
using ConcreteEngine.Core.Engine.Assets;
using ConcreteEngine.Core.Engine.Assets.Data;
using ConcreteEngine.Engine.Assets.Utils;

namespace ConcreteEngine.Engine.Assets;

public sealed partial class AssetStore
{
    public AssetsMetaInfo GetMetaSnapshot<TAsset>() where TAsset : AssetObject => GetAssetList<TAsset>().ToSnapshot();

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public AssetCollection<T> GetAssetList<T>() where T : AssetObject =>
        (AssetCollection<T>)_collections[AssetKindUtils.ToAssetIndex(typeof(T))];

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public AssetCollection GetAssetList(AssetKind kind) => _collections[kind.ToIndex()];

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public AssetObject Get(AssetId assetId) => _assets[assetId.Index()];

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public T Get<T>(AssetId assetId) where T : AssetObject
    {
        if (TryGet(assetId, out var value) && value is T tValue) return tValue;
        throw new KeyNotFoundException($"Asset '{assetId.Value}' not found or incorrect type.");
    }

    public T GetByName<T>(string name) where T : AssetObject
    {
        if (TryGetByName<T>(name, out var value)) return value!;
        throw new KeyNotFoundException($"Asset GetByName '{name}' not found or incorrect type.");
    }

    public T GetByGid<T>(Guid gid) where T : AssetObject
    {
        if (TryGetByGuid<T>(gid, out var value)) return value!;
        throw new KeyNotFoundException($"Asset GetByGid '{gid}' not found or incorrect type.");
    }

    public bool TryGet(AssetId assetId, out AssetObject asset)
    {
        asset = null!;
        var index = assetId.Index();
        if ((uint)index > (uint)_assets.Length) return false;
        return (asset = _assets[index]) != null;
    }

    public bool TryGet<T>(AssetId assetId, out T asset) where T : AssetObject
    {
        asset = null!;
        if (!TryGet(assetId, out var res) || res is not T tRes) return false;
        asset = tRes;
        return true;
    }

    public bool TryGetByName<T>(string name, out T asset) where T : AssetObject
    {
        asset = null!;
        if (!TryGetByName(name, typeof(T), out var res) || res is not T tRes) return false;
        asset = tRes;
        return true;
    }

    public bool TryGetByGuid<T>(Guid guid, out T asset) where T : AssetObject
    {
        asset = null!;
        if (!TryGetByGuid(guid, out var res) || res is not T tRes) return false;
        asset = tRes;
        return true;
    }


    public bool TryGetByGuid(Guid gid, out AssetObject asset)
    {
        asset = null!;
        return TryGetIdByGuid(gid, out var assetId) && TryGet(assetId, out asset);
    }

    public bool TryGetIdByGuid(Guid gid, out AssetId assetId) => _byGid.TryGetValue(gid, out assetId);

    public bool TryGetByName(string name, Type type, out AssetObject asset)
    {
        asset = null!;
        if (!_byName.TryGetValue((type, name), out var id)) return false;
        if (!TryGet(id, out var objT)) return false;
        asset = objT;
        return true;
    }


    public void ExtractList<TAsset, TData>(List<TData> list, Func<TAsset, TData> transform)
        where TAsset : AssetObject where TData : class
    {
        foreach (var asset in _assets)
        {
            if (asset is not TAsset typedAsset) continue;
            var it = transform(typedAsset);
            if (it == null!) continue;
            list.Add(it);
        }
    }

    public void ExtractSpan<TAsset, TData>(Span<TData> span, Func<TAsset, TData> transform) where TAsset : AssetObject
    {
        var idx = 0;
        var list = GetAssetList<TAsset>();
        foreach (var asset in list.GetAssetSpan())
        {
            span[idx++] = transform(asset);
            if (idx >= span.Length) break;
        }
    }

    public void Process<TAsset>(Action<TAsset> action) where TAsset : AssetObject
    {
        var list = GetAssetList<TAsset>();
        foreach (var asset in list.GetAssetSpan())
            action(asset);
    }
}