using ConcreteEngine.Engine.Assets.Data;
using ConcreteEngine.Engine.Assets.Internal;
using ConcreteEngine.Engine.Assets.Utils;
using ConcreteEngine.Engine.Metadata;
using ConcreteEngine.Engine.Metadata.Asset;

namespace ConcreteEngine.Engine.Assets;

public sealed partial class AssetStore
{
    public AssetStoreMeta GetMetaSnapshot<TAsset>() where TAsset : AssetObject => GetAssetList<TAsset>().ToSnapshot();

    internal AssetList<T> GetAssetList<T>() where T : AssetObject =>
        (AssetList<T>)_assetLists[AssetEnums.ToAssetIndex<T>()];


    public T Get<T>(AssetId assetId) where T : AssetObject
    {
        if (TryGet(assetId, out var value) && value is T tValue) return tValue;
        throw new InvalidCastException($"Asset '{assetId.Value}' not found or incorrect type.");
    }


    public T GetByName<T>(string name) where T : AssetObject
    {
        if (TryGetByName<T>(name, out var value)) return value!;
        throw new InvalidCastException($"Asset GetByName '{name}' not found or incorrect type.");
    }
    
    public T GetByGid<T>(string name) where T : AssetObject
    {
        if (TryGetByName<T>(name, out var value)) return value!;
        throw new InvalidCastException($"Asset GetByName '{name}' not found or incorrect type.");
    }

    public bool TryGet(AssetId assetId, out AssetObject asset) => _assets.TryGetValue(assetId, out asset!);

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
        if (!_byName.TryGetValue(new AssetKey(type, name), out var id)) return false;
        if (!_assets.TryGetValue(id, out var objT)) return false;
        asset = objT;
        return true;
    }
    
    public ReadOnlySpan<AssetFileId> GetFileIds(AssetId assetId) 
    {
        if (TryGetFileIds(assetId, out var fileIds)) return fileIds;
        throw new InvalidCastException($"Asset TryGetFileIds '{assetId}' not found or incorrect type.");
        
    }

    public bool TryGetFileEntry(AssetFileId id, out AssetFileSpec entry) => _files.TryGetValue(id, out entry!);

    internal bool TryGetFileIds(AssetId id, out ReadOnlySpan<AssetFileId> fileIds)
    {
        fileIds = ReadOnlySpan<AssetFileId>.Empty;
        if (_fileBindings.TryGetValue(id, out var res)) fileIds = res;
        return !fileIds.IsEmpty;
    }
    

    public void ExtractList<TAsset, TData>(List<TData> list, Func<TAsset, TData> transform)
        where TAsset : AssetObject where TData : class
    {
        foreach (var asset in _assets.Values)
        {
            if (asset is not TAsset typedAsset) continue;
            var it = transform(typedAsset);
            if (it == null!) continue;
            list.Add(it);
        }
    }

    public void FillSpan<TAsset, TData>(Span<TData> span, Action<TAsset, Span<TData>> transform)
        where TAsset : AssetObject where TData : unmanaged
    {
        var list = GetAssetList<TAsset>();
        foreach (var asset in list.Asset)
            transform(asset, span);
    }

    public void ExtractSpan<TAsset, TData>(Span<TData> span, Func<TAsset, TData> transform)
        where TAsset : AssetObject where TData : unmanaged
    {
        var idx = 0;
        var list = GetAssetList<TAsset>();
        foreach (var asset in list.Asset)
        {
            span[idx++] = transform(asset);
            if (idx >= span.Length) break;
        }
    }

    public void Process<TAsset>(Action<TAsset> action) where TAsset : AssetObject
    {
        var list = GetAssetList<TAsset>();
        foreach (var asset in list.Asset)
            action(asset);
    }
}