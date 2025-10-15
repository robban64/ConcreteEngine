#region

using ConcreteEngine.Core.Assets.Data;
using ConcreteEngine.Core.Assets.Descriptors;

#endregion

namespace ConcreteEngine.Core.Assets;

public interface IAssetStore
{
    T GetByRef<T>(AssetRef<T> id) where T : AssetObject;
    T GetByName<T>(string name) where T : AssetObject;
    bool TryGetByName<T>(string name, out T? asset) where T : AssetObject;

    AssetTypeMetaSnapshot GetMetaSnapshot<TAsset>() where TAsset : AssetObject;

    void ExtractList<TAsset, TData>(List<TData> list, Func<TAsset, TData> transform)
        where TAsset : AssetObject;

    void ExtractSpan<TAsset, TData>(Span<TData> span, Func<TAsset, TData> transform)
        where TAsset : AssetObject where TData : unmanaged;
}

internal sealed class AssetStore : IAssetStore
{
    private int _assetId = 0;
    private int _assetFileId = 0;
    private AssetId MakeAssetId() => new(++_assetId);
    private AssetFileId MakeAssetFileId() => new(++_assetFileId);


    // AssetObject abstract class
    private readonly Dictionary<AssetId, AssetObject> _assets = new(32);
    private readonly Dictionary<AssetFileId, AssetFileEntry> _files = new(32);
    private readonly Dictionary<AssetId, AssetFileId[]> _bindings = new(32);
    private readonly Dictionary<AssetKey, AssetId> _names = new(32);

    private readonly Dictionary<Type, AssetTypeMeta> _typeMeta = new(8);

    internal AssetStore()
    {
    }

    public T GetByRef<T>(AssetRef<T> assetRef) where T : AssetObject
    {
        if (TryGetByRef(assetRef, out var value)) return value!;

        throw new InvalidCastException($"Asset '{assetRef.Value}' not found or incorrect type.");
    }

    public T GetByName<T>(string name) where T : AssetObject
    {
        if (TryGetByName<T>(name, out var value)) return value!;

        throw new InvalidCastException($"Asset '{name}' not found or incorrect type.");
    }

    public bool TryGetByRef<T>(AssetRef<T> assetRef, out T? asset) where T : AssetObject
    {
        if (_assets.TryGetValue(assetRef, out var obj) && obj is T t)
        {
            asset = t;
            return true;
        }

        asset = null;
        return false;
    }

    public bool TryGetByName<T>(string name, out T? asset) where T : AssetObject
    {
        asset = null;
        if (!_names.TryGetValue(AssetKey.For<T>(name), out var id)) return false;
        if (!_assets.TryGetValue(id, out var objT)) return false;
        if (objT is T t)
        {
            asset = t;
            return true;
        }

        return false;
    }

    private ReadOnlySpan<AssetFileId> GetFileIds(AssetId id)
    {
        if (_bindings.TryGetValue(id, out var arr)) return arr!;
        throw new InvalidCastException($"Asset '{id}' not found or incorrect type.");
    }


    public AssetTypeMetaSnapshot GetMetaSnapshot<TAsset>() where TAsset : AssetObject =>
        _typeMeta[typeof(TAsset)].ToSnapshot();


    public void ExtractList<TAsset, TData>(List<TData> list, Func<TAsset, TData> transform)
        where TAsset : AssetObject
    {
        foreach (var asset in _assets.Values)
        {
            if (asset is TAsset typedAsset) list.Add(transform(typedAsset));
        }
    }

    public void ExtractSpan<TAsset, TData>(Span<TData> span, Func<TAsset, TData> transform)
        where TAsset : AssetObject where TData : unmanaged
    {
        var idx = 0;
        foreach (var asset in _assets.Values)
        {
            if (asset is TAsset typedAsset) span[idx++] = transform(typedAsset);
            if (idx >= span.Length) break;
        }
    }


    internal TAsset Register<TAsset, TDesc>(TDesc descriptor, AssetAssembleDel<TAsset, TDesc> factory)
        where TAsset : AssetObject where TDesc : class, IAssetDescriptor
    {
        var id = MakeAssetId();
        var asset = factory(id, descriptor, this);
        RegisterInternal(id, asset, ReadOnlySpan<AssetFileSpec>.Empty);
        return asset;
    }

    internal TAsset RegisterWithFiles<TAsset, TDesc>(
        TDesc descriptor,
        AssetFileAssembleDel<TAsset, TDesc> factory)
        where TAsset : AssetObject where TDesc : class, IAssetDescriptor
    {
        var id = MakeAssetId();
        var asset = factory(id, descriptor, out var fileSpecs);
        ArgumentNullException.ThrowIfNull(fileSpecs, nameof(fileSpecs));
        RegisterInternal(id, asset, fileSpecs);
        return asset;
    }

    private void RegisterInternal<TAsset>(AssetId id, TAsset asset, ReadOnlySpan<AssetFileSpec> fileSpecs)
        where TAsset : AssetObject
    {
        ArgumentNullException.ThrowIfNull(asset);
        ArgumentOutOfRangeException.ThrowIfLessThanOrEqual(id.Value, 0);
        ArgumentOutOfRangeException.ThrowIfNotEqual(asset.RawId.Value, id.Value);

        if (!_assets.TryAdd(id, asset))
            throw new InvalidOperationException($"Asset '{asset.Name}' is already registered by id.");

        if (!_names.TryAdd(new AssetKey(typeof(TAsset), asset.Name), id))
            throw new InvalidOperationException($"Asset '{asset.Name}' is already registered by type/name.");

        IncrementTypeCount<TAsset>(fileSpecs.Length);

        if (fileSpecs.Length > 0)
            RegisterBindingsInternal(id, fileSpecs);
    }

    private void RegisterBindingsInternal(AssetId assetId, ReadOnlySpan<AssetFileSpec> fileSpecs)
    {
        var fileIds = new AssetFileId[fileSpecs.Length];

        for (int i = 0; i < fileSpecs.Length; i++)
        {
            ref readonly var spec = ref fileSpecs[i];
            var fileId = new AssetFileId(_assetFileId++);
            fileIds[i] = fileId;
            _files.Add(fileId, new AssetFileEntry(fileId, in spec));
        }

        _bindings.Add(assetId, fileIds);
    }

    private void IncrementTypeCount<TAsset>(int files) where TAsset : AssetObject
    {
        if (!_typeMeta.TryGetValue(typeof(TAsset), out var meta))
            _typeMeta[typeof(TAsset)] = meta = new AssetTypeMeta(typeof(TAsset));

        meta.Increment(files);
    }

    private readonly record struct AssetKey(Type RegistryType, string Name)
    {
        public static AssetKey For<T>(string name) where T : AssetObject => new(typeof(T), name);
    }
}