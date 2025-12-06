#region

using ConcreteEngine.Common;
using ConcreteEngine.Engine.Assets.Data;
using ConcreteEngine.Engine.Assets.Descriptors;

#endregion

namespace ConcreteEngine.Engine.Assets;

public interface IAssetStore
{
    T GetByRef<T>(AssetRef<T> id) where T : AssetObject;
    T GetByName<T>(string name) where T : AssetObject;

    bool TryGetByRef<T>(AssetRef<T> assetRef, out T? asset) where T : AssetObject;
    bool TryGetByName<T>(string name, out T? asset) where T : AssetObject;

    AssetTypeMetaSnapshot GetMetaSnapshot<TAsset>() where TAsset : AssetObject;

    void ExtractList<TAsset, TData>(List<TData> list, Func<TAsset, TData> transform)
        where TAsset : AssetObject where TData : class;

    void ExtractSpan<TAsset, TData>(Span<TData> span, Func<TAsset, TData> transform)
        where TAsset : AssetObject where TData : unmanaged;

    void Process<TAsset>(Action<TAsset> action) where TAsset : AssetObject;
}

internal sealed class AssetStore : IAssetStore
{
    private const int DefaultCap = 256;

    private int _assetId = 0;
    private int _assetFileId = 0;
    private AssetId MakeAssetId() => new(++_assetId);
    private AssetFileId MakeAssetFileId() => new(++_assetFileId);

    private readonly Dictionary<AssetId, AssetObject> _assets = new(DefaultCap);
    private readonly Dictionary<AssetFileId, AssetFileEntry> _files = new(DefaultCap);
    private readonly Dictionary<AssetId, AssetFileId[]> _bindings = new(DefaultCap);
    private readonly Dictionary<AssetKey, AssetId> _names = new(DefaultCap);

    private readonly Dictionary<Type, AssetTypeMeta> _typeMeta = new(8);

    private readonly Dictionary<Guid, AssetId> _byEmbedded = new(8);

    public int Count => _assetId;
    public int FileCount => _files.Count;
    public int Capacity => _assets.Capacity;
    public int TypeCount => _typeMeta.Count;

    internal AssetStore()
    {
    }

    public int GetAssetCount<TAsset>() where TAsset : AssetObject => _typeMeta[typeof(TAsset)].Count;
    internal IReadOnlyDictionary<Type, AssetTypeMeta> GetAssetTypeMeta() => _typeMeta;
    internal Dictionary<AssetId, AssetObject>.ValueCollection AssetValues => _assets.Values;

    public AssetTypeMetaSnapshot GetMetaSnapshot<TAsset>() where TAsset : AssetObject =>
        _typeMeta[typeof(TAsset)].ToSnapshot();

    internal AssetTypeMetaSnapshot GetMetaSnapshot(Type type) => _typeMeta[type].ToSnapshot();


    public bool TryGetFileEntry(AssetFileId id, out AssetFileEntry? entry) => _files.TryGetValue(id, out entry);

    internal bool TryGetFileIds(AssetId id, out ReadOnlySpan<AssetFileId> fileIds)
    {
        if (_bindings.TryGetValue(id, out var res))
        {
            fileIds = res;
            return true;
        }

        fileIds = ReadOnlySpan<AssetFileId>.Empty;
        return false;
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

    internal bool TryGetByAssetId(AssetId assetId, out AssetObject? asset)
    {
        if (_assets.TryGetValue(assetId, out var obj))
        {
            asset = obj;
            return true;
        }

        asset = null;
        return false;
    }

    public bool TryGetByName<T>(string name, out T? asset) where T : AssetObject
    {
        if (TryGetByName(name, typeof(T), out var objT) && objT is T t)
        {
            asset = t;
            return true;
        }

        asset = null;
        return false;
    }

    internal bool TryGetByName(string name, Type type, out AssetObject? asset)
    {
        asset = null;
        if (!_names.TryGetValue(new AssetKey(type, name), out var id)) return false;
        if (!_assets.TryGetValue(id, out var objT)) return false;
        asset = objT;
        return true;
    }

    internal bool TryGetByEmbeddedGid<TAsset>(Guid gid, out TAsset asset) where TAsset : AssetObject
    {
        asset = null!;
        if (!_byEmbedded.TryGetValue(gid, out var assetId)) return false;
        if (!_assets.TryGetValue(assetId, out var obj) || obj is not TAsset tAsset) return false;

        asset = tAsset;
        return true;
    }

    public void ExtractList<TAsset, TData>(List<TData> list, Func<TAsset, TData> transform)
        where TAsset : AssetObject where TData : class
    {
        foreach (var asset in _assets.Values)
        {
            if (asset is not TAsset typedAsset) continue;
            var it = transform(typedAsset);
            // ReSharper disable once ConditionIsAlwaysTrueOrFalseAccordingToNullableAPIContract
            if (it is null) continue;
            list.Add(it);
        }
    }
 
    public void FillSpan<TAsset, TData>(Span<TData> span, Action<TAsset, Span<TData>> transform)
        where TAsset : AssetObject where TData : unmanaged
    {
        foreach (var asset in _assets.Values)
        {
            if (asset is TAsset typedAsset) transform(typedAsset, span);
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

    public void ExtractMeta(Span<AssetTypeMetaSnapshot> span)
    {
        var idx = 0;
        foreach (var meta in _typeMeta.Values)
        {
            span[idx++] = meta.ToSnapshot();
            if (idx >= span.Length) break;
        }
    }


    public ReadOnlySpan<string> GetStoreNames()
    {
        var names = new string[Count];
        var idx = 0;
        foreach (var it in _typeMeta.Keys)
            names[idx++] = it.Name;

        return names;
    }

    public void Process<TAsset>(Action<TAsset> action) where TAsset : AssetObject
    {
        foreach (var asset in _assets.Values)
        {
            if (asset is TAsset typedAsset) action(typedAsset);
        }
    }


    public void Reload<TAsset>(TAsset asset, AssetFileReloadDel<TAsset> factory) where TAsset : AssetObject
    {
        var gen = asset.Generation;

        TryGetFileIds(asset.RawId, out var fileIds);
        var files = new AssetFileEntry[fileIds.Length];
        for (var i = 0; i < fileIds.Length; i++)
            files[i] = _files[fileIds[i]];

        factory(asset, files, out var fileSpecs);
        InvalidOpThrower.ThrowIf(gen != asset.Generation, nameof(asset.Generation));
        InvalidOpThrower.ThrowIf(files.Length != fileSpecs.Length, nameof(fileSpecs.Length));

        asset.BumpGeneration();
        if (fileSpecs.Length > 0) RegisterExistingBindings(asset.RawId, files, fileSpecs);
    }

    internal TAsset Register<TAsset, TDesc>(TDesc descriptor, AssetAssembleDel<TAsset, TDesc> factory)
        where TAsset : AssetObject where TDesc : class, IAssetDescriptor
    {
        var id = MakeAssetId();
        var asset = factory(id, descriptor, this);
        RegisterInternal(id, asset, ReadOnlySpan<AssetFileSpec>.Empty);
        return asset;
    }

    internal TAsset RegisterWithFiles<TAsset, TDesc>(TDesc descriptor, bool isCoreAsset,
        AssetFileAssembleDel<TAsset, TDesc> factory)
        where TAsset : AssetObject where TDesc : class, IAssetDescriptor
    {
        var id = MakeAssetId();
        var asset = factory(id, descriptor, isCoreAsset, out var fileSpecs);
        ArgumentNullException.ThrowIfNull(fileSpecs);
        RegisterInternal(id, asset, fileSpecs);
        return asset;
    }


    internal TAsset RegisterWithEmbedded<TAsset, TDesc>(
        TDesc descriptor,
        bool isCoreAsset,
        AssetWithEmbeddedDel<TAsset, TDesc> factory,
        Action<ReadOnlySpan<IAssetEmbeddedDescriptor>> enqueueEmbedded)
        where TAsset : AssetObject
        where TDesc : class, IAssetDescriptor
    {
        var id = MakeAssetId();
        var asset = factory(id, descriptor, isCoreAsset, enqueueEmbedded, out var fileSpecs);
        ArgumentNullException.ThrowIfNull(fileSpecs);

        RegisterInternal(id, asset, fileSpecs);
        return asset;
    }

    internal TAsset RegisterEmbedded<TAsset, TEmbedded>(
        TEmbedded embedded,
        EmbeddedAssembleDel<TAsset, TEmbedded> factory)
        where TAsset : AssetObject where TEmbedded : class, IAssetEmbeddedDescriptor
    {
        ArgumentNullException.ThrowIfNull(embedded);
        ArgumentNullException.ThrowIfNull(embedded.FileSpec);
        ArgumentOutOfRangeException.ThrowIfZero(embedded.FileSpec.Length);

        if (_byEmbedded.ContainsKey(embedded.GId))
            throw new InvalidOperationException($"Embedded resource is already registered. {embedded.GId}");

        var id = MakeAssetId();
        var asset = factory(id, embedded, this);
        _byEmbedded.Add(embedded.GId, id);
        asset.Name = embedded.AssetName;
        asset.IsEmbedded = true;
        RegisterInternal(id, asset, embedded.FileSpec);
        //Logger.LogString(LogScope.Assets, $"{asset.Name} - Embedded {typeof(TAsset).Name} loaded");
        //Console.WriteLine( $"{asset.Name} - Embedded {embedded.FileSpec[0].Source} loaded");
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
            RegisterNewBindings(id, fileSpecs);
    }

    private void RegisterNewBindings(AssetId assetId, ReadOnlySpan<AssetFileSpec> fileSpecs)
    {
        var fileIds = new AssetFileId[fileSpecs.Length];

        for (var i = 0; i < fileSpecs.Length; i++)
        {
            ref readonly var spec = ref fileSpecs[i];
            var fileId = new AssetFileId(_assetFileId++);
            fileIds[i] = fileId;
            _files.Add(fileId, new AssetFileEntry(fileId, spec));
        }

        _bindings.Add(assetId, fileIds);
    }

    private void RegisterExistingBindings(AssetId assetId, ReadOnlySpan<AssetFileEntry> prevFiles,
        ReadOnlySpan<AssetFileSpec> fileSpecs)
    {
        for (var i = 0; i < fileSpecs.Length; i++)
        {
            ref readonly var spec = ref fileSpecs[i];
            var file = prevFiles[i];
            _files[file.Id] = new AssetFileEntry(file.Id, spec);
        }
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