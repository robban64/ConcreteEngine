using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using ConcreteEngine.Core.Common;
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
        if ((uint)index >= (uint)_assets.Length)
        {
            asset = null;
            return false;
        }
        if (_assets[index] is T tAsset && tAsset.Id == id)
        {
            asset = tAsset;
            return true;
        }
        asset = null;
        return false;
    }

    public bool TryGetByName<T>(string name, [NotNullWhen(true)] out T? asset) where T : AssetObject
    {
        if (!GetTypeStore(typeof(T)).TryGetByName(name, out var assetId))
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
        new(GetTypeStore(AssetKindUtils.ToAssetKind(typeof(T))).AsSpan(), _assets.AsSpan());
}