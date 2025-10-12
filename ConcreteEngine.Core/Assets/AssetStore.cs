using ConcreteEngine.Common;
using ConcreteEngine.Core.Assets.Data;

namespace ConcreteEngine.Core.Assets;

public sealed class AssetStore
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


    // public Func<int> IdProvider() => () => new AssetBuildRecord();
/*
    internal AssetId RegisterAsset(AssetObject asset, AssetFileSet fileSet)
    {
        var id = new AssetId(_assetId++);
        _assets[id] = asset;
        _bindings[id] = fileSet;

        if (!_names.TryAdd(new AssetKey(asset.GetType(),asset.Name), id))
            throw new InvalidOperationException($"Asset '{asset.Name}' is already exists.");

        return id;
    }

    internal AssetFileId RegisterFile(AssetFileEntry entry)
    {
        var id = new AssetFileId(_assetFileId);
        _files[id] = entry;
        _assetFileId++;
        return id;
    }
*/
    internal TAsset Register<TAsset>(AssetAssembleDel<TAsset> factory) where TAsset : AssetObject
    {
        var id = new AssetId(_assetId++);
        var asset = factory(id);
        
        InvalidOpThrower.ThrowIfNull(asset);
        InvalidOpThrower.ThrowIfNot(asset.Id == id);

        if (!_names.TryAdd(new AssetKey(typeof(TAsset), asset.Name), id))
            throw new InvalidOperationException($"Asset '{asset.Name}' is already exists.");

        _assets.Add(id, asset);
        return asset;
    }

    internal TAsset Register<TAsset>(AssetFileAssembleDel<TAsset> factory) where TAsset : AssetObject
    {
        var id = new AssetId(_assetId++);
        var asset = factory(id, (assetId, specs) =>
        {
            var fileIds = new AssetFileId[specs.Length];

            for (int i = 0; i < specs.Length; i++)
            {
                ref readonly var spec = ref specs[i];
                var fileId = new AssetFileId(_assetFileId++);
                fileIds[i] = fileId;
                _files.Add(fileId, new AssetFileEntry(fileId, in spec));
            }

            _bindings.Add(assetId, fileIds);
            return fileIds;
        });
        
        InvalidOpThrower.ThrowIfNull(asset);
        InvalidOpThrower.ThrowIfNot(asset.Id == id);

        if (!_names.TryAdd(new AssetKey(typeof(TAsset), asset.Name), id))
            throw new InvalidOperationException($"Asset '{asset.Name}' is already exists.");

        _assets.Add(id, asset);
        return asset;
    }

    private AssetFileId[] RegisterFiles<TManifest>(AssetId assetId, TManifest manifest,
        ReadOnlySpan<AssetFileSpec> specs)
        where TManifest : class, IAssetManifestRecord
    {
        var fileIds = new AssetFileId[specs.Length];

        for (int i = 0; i < specs.Length; i++)
        {
            ref readonly var spec = ref specs[i];
            var fileId = new AssetFileId(_assetFileId++);
            fileIds[i] = fileId;
            _files.Add(fileId, new AssetFileEntry(fileId, in spec));
        }

        _bindings.Add(assetId, fileIds);
        return fileIds;
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
}