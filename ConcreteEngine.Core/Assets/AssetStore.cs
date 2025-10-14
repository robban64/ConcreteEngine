using ConcreteEngine.Common;
using ConcreteEngine.Core.Assets.Data;

namespace ConcreteEngine.Core.Assets;

public interface IAssetStore
{
    T Get<T>(AssetId id) where T : AssetObject;
    T Get<T>(string name) where T : AssetObject;
    bool TryGet<T>(string name, out T? asset) where T : AssetObject;

    List<TAsset> ExtractAllObjects<TAsset>() where TAsset : AssetObject;

    List<TData> ExtractData<TAsset, TData>(Func<TAsset, TData> transform)
        where TAsset : AssetObject where TData : unmanaged;
}

internal sealed class AssetStore : IAssetStore
{
    private int _assetId = 0;
    private int _assetFileId = 0;

    private readonly record struct AssetKey(Type RegistryType, string Name)
    {
        public static AssetKey For<T>(string name) where T : AssetObject => new(typeof(T), name);
    }

    // AssetObject abstract class
    private readonly Dictionary<AssetId, AssetObject> _assets = new(32);
    private readonly Dictionary<AssetFileId, AssetFileEntry> _files = new(32);
    private readonly Dictionary<AssetId, AssetFileId[]> _bindings = new(32);
    private readonly Dictionary<AssetKey, AssetId> _names = new(32);

    internal AssetStore()
    {
    }

    public T Get<T>(AssetId id) where T : AssetObject
    {
        var asset = _assets[id];
        if(asset is not T assetObject) throw new InvalidOperationException();
        return assetObject;
    }

    public T Get<T>(string name) where T : AssetObject
    {
        if (TryGet<T>(name, out var value)) return value!;

        throw new InvalidCastException($"Asset '{name}' not found or incorrect type.");
    }

    public bool TryGet<T>(string name, out T? asset) where T : AssetObject
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

    public List<TAsset> ExtractAllObjects<TAsset>() where TAsset : AssetObject
    {
        var result = new List<TAsset>(8);
        foreach (var asset in _assets.Values)
        {
            if (asset is TAsset typedAsset) result.Add(typedAsset);
        }

        return result;
    }

    public List<TData> ExtractData<TAsset, TData>(Func<TAsset, TData> transform)
        where TAsset : AssetObject where TData : unmanaged
    {
        var result = new List<TData>(8);
        foreach (var asset in _assets.Values)
        {
            if (asset is TAsset typedAsset)
                result.Add(transform(typedAsset));
        }

        return result;
    }

    internal TAsset Register<TAsset, TManifest>(TManifest manifest, AssetAssembleDel<TAsset, TManifest> factory)
        where TAsset : AssetObject where TManifest : class, IAssetManifestRecord
    {
        var id = new AssetId(_assetId++);
        var asset = factory(id, manifest, this);
        RegisterInternal(id, asset, ReadOnlySpan<AssetFileSpec>.Empty);
        return asset;
    }

    internal TAsset RegisterWithFiles<TAsset, TManifest>(TManifest manifest,
        AssetFileAssembleDel<TAsset, TManifest> factory)
        where TAsset : AssetObject where TManifest : class, IAssetManifestRecord
    {
        var id = new AssetId(_assetId++);
        var asset = factory(id, manifest, out var fileSpecs);
        ArgumentNullException.ThrowIfNull(fileSpecs, nameof(fileSpecs));
        RegisterInternal(id, asset, fileSpecs);
        return asset;
    }

    private void RegisterInternal<TAsset>(AssetId id, TAsset asset, ReadOnlySpan<AssetFileSpec> fileSpecs)
        where TAsset : AssetObject
    {
        ArgumentNullException.ThrowIfNull(asset);
        ArgumentOutOfRangeException.ThrowIfEqual(asset.Id.Value, id.Value);

        if (!_names.TryAdd(new AssetKey(typeof(TAsset), asset.Name), id))
            throw new InvalidOperationException($"Asset '{asset.Name}' is already exists.");

        _assets.Add(id, asset);

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
}