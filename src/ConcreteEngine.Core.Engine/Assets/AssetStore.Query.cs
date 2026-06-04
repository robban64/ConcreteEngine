using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using ConcreteEngine.Core.Common;
using ConcreteEngine.Core.Engine.Assets.Data;
using ConcreteEngine.Core.Engine.Assets.Utils;

namespace ConcreteEngine.Core.Engine.Assets;

public sealed partial class AssetStore
{
    public AssetsMetaInfo GetMetaSnapshot<TAsset>() where TAsset : AssetObject =>
        GetTypeStore(AssetKindUtils.ToAssetKind(typeof(TAsset))).ToSnapshot();

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool Has(AssetId id)
    {
        var index = id.Index();
        return (uint)index < (uint)_assets.Capacity && _assets[index]?.Id == id;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal T GetUnsafe<T>(int id) where T : AssetObject => (T)_assets[id - 1]!;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public AssetObject Get(AssetId id)
    {
        var it = _assets[id.Index()];
        if (it?.Id != id) Throwers.InvalidHandle(id);
        return it;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public T Get<T>(AssetId id) where T : AssetObject
    {
        var asset = Get(id);
        if (asset is T tAsset) return tAsset;
        Throwers.InvalidOperation("Invalid asset type");
        return null;
    }

    public T GetByName<T>(string name) where T : AssetObject
    {
        if (TryGetByName<T>(name, out var value)) return value;
        Throwers.KeyNotFound(name);
        return null;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool TryGet(AssetId id, [NotNullWhen(true)] out AssetObject? asset)
    {
        return _assets.TryGet(id.Index(), out asset) && asset.Id == id;
    }

    public bool TryGet<T>(AssetId id, [NotNullWhen(true)] out T? asset) where T : AssetObject
    {
        asset = !TryGet(id, out var res) || res is not T tRes ? null : tRes;
        return asset != null;
    }

    public bool TryGetByName<T>(string name, [NotNullWhen(true)] out T? asset) where T : AssetObject
    {
        asset = !TryGetByName(name, typeof(T), out var res) || res is not T tRes ? null : tRes;
        return asset != null;
    }

    public bool TryGetByName(string name, Type type, [NotNullWhen(true)] out AssetObject? asset)
    {
        if (!GetTypeStore(type).TryGetByName(name, out var assetId))
        {
            asset = null;
            return false;
        }
        asset = !TryGet(assetId, out var objT) ? null : objT;
        return asset != null;
    }

    public bool TryGetByGuid<T>(Guid guid, [NotNullWhen(true)] out T? asset) where T : AssetObject
    {
        asset = !TryGetByGuid(guid, out var res) || res is not T tRes ? null : tRes;
        return asset != null;
    }

    public bool TryGetByGuid(Guid gid, [NotNullWhen(true)] out AssetObject? asset)
    {
        asset = !_byGid.TryGetValue(gid, out var assetId) || !TryGet(assetId, out var res) ? null : res;
        return asset != null;
    }

    public bool TryGetIdByGuid(Guid gid, out AssetId id) => _byGid.TryGetValue(gid, out id);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public AssetEnumerator GetAssetEnumerator(AssetKind kind) => new(GetTypeStore(kind).AsSpan(), _assets.AsSpan());

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public AssetEnumerator<T> GetAssetEnumerator<T>() where T : AssetObject =>
        new(GetTypeStore(AssetKindUtils.ToAssetKind(typeof(T))).AsSpan(), _assets.AsSpan());
}