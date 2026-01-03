using ConcreteEngine.Core.Common;
using ConcreteEngine.Core.Common.Memory;
using ConcreteEngine.Engine.Assets.Data;
using ConcreteEngine.Engine.Assets.Descriptors;
using ConcreteEngine.Engine.Assets.Materials;
using ConcreteEngine.Engine.Assets.Models;
using ConcreteEngine.Engine.Assets.Shaders;
using ConcreteEngine.Engine.Assets.Textures;
using ConcreteEngine.Engine.Assets.Utils;
using ConcreteEngine.Engine.Metadata;

namespace ConcreteEngine.Engine.Assets;

public interface IAssetList
{
    int Count { get; }
}

internal sealed class AssetList<T>() : IAssetList where T : AssetObject
{
    public readonly List<T> Assets = [];
    
    public int Count { get; }
    public int FileCount { get; private set; }
}

public sealed class AssetStore
{
    private const int DefaultCap = 256;

    private static int _assetId;
    private static int _assetFileId;
    private static AssetId MakeAssetId() => new(++_assetId);
    private static AssetFileId MakeAssetFileId() => new(++_assetFileId);

    private readonly IAssetList[] _assetLists = new IAssetList[EnumCache<AssetKind>.Count - 1];

    private readonly Dictionary<AssetId, AssetObject> _assets = new(DefaultCap);
    private readonly Dictionary<Guid, AssetObject> _assetByGid = new(DefaultCap);

    private readonly Dictionary<AssetFileId, AssetFileEntry> _files = new(DefaultCap);
    private readonly Dictionary<AssetId, AssetFileId[]> _bindings = new(DefaultCap);
    private readonly Dictionary<AssetKey, AssetId> _names = new(DefaultCap);

    private readonly Dictionary<Type, AssetStoreTypeMeta> _typeMeta = new(8);
    private readonly Dictionary<Guid, AssetId> _byEmbedded = new(8);

    public int Count => _assetId;
    public int FileCount => _files.Count;
    public int Capacity => _assets.Capacity;
    public int StoreCount => _typeMeta.Count;

    internal AssetStore()
    {
        if (_assetId > 0 || _assetFileId > 0) throw new InvalidOperationException();
        
        _assetLists[(int)AssetKind.Shader - 1] = new AssetList<Shader>();
        _assetLists[(int)AssetKind.Model - 1] = new AssetList<Model>();
        _assetLists[(int)AssetKind.Texture2D - 1] = new AssetList<Texture2D>();
        _assetLists[(int)AssetKind.TextureCubeMap - 1] = new AssetList<CubeMap>();
        _assetLists[(int)AssetKind.MaterialTemplate - 1] = new AssetList<MaterialTemplate>();
    }

    public int GetAssetCount<TAsset>() where TAsset : AssetObject => _typeMeta[typeof(TAsset)].Count;
    internal IReadOnlyDictionary<Type, AssetStoreTypeMeta> GetAssetTypeMeta() => _typeMeta;
    internal Dictionary<AssetId, AssetObject>.ValueCollection AssetValues => _assets.Values;

    public AssetStoreMeta GetMetaSnapshot<TAsset>() where TAsset : AssetObject =>
        _typeMeta[typeof(TAsset)].ToSnapshot();

    internal AssetStoreMeta GetMetaSnapshot(Type type) => _typeMeta[type].ToSnapshot();


    internal AssetList<T> GetAssetList<T>() where T : AssetObject
    {
        return (AssetList<T>)_assetLists[(int)AssetEnums.ToAssetKind<T>() - 1];
    }

    public T GetByRef<T>(AssetRef<T> assetRef) where T : AssetObject
    {
        if (TryGetByRef(assetRef, out var value)) return value!;
        throw new InvalidCastException($"Asset '{assetRef.Id.Value}' not found or incorrect type.");
    }

    public T GetByName<T>(string name) where T : AssetObject
    {
        if (TryGetByName<T>(name, out var value)) return value!;
        throw new InvalidCastException($"Asset '{name}' not found or incorrect type.");
    }

    public bool TryGetByRef<T>(AssetRef<T> assetRef, out T? asset) where T : AssetObject
    {
        asset = null!;
        if (!TryGetByAssetId(assetRef, out var res) || res is not T tRes) return false;
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

    public bool TryGetFileEntry(AssetFileId id, out AssetFileEntry? entry) => _files.TryGetValue(id, out entry);

    internal bool TryGetFileIds(AssetId id, out ReadOnlySpan<AssetFileId> fileIds)
    {
        fileIds = ReadOnlySpan<AssetFileId>.Empty;
        if (_bindings.TryGetValue(id, out var res)) fileIds = res;
        return !fileIds.IsEmpty;
    }

    internal bool TryGetByAssetId(AssetId assetId, out AssetObject? asset) => _assets.TryGetValue(assetId, out asset);

    internal bool TryGetByName(string name, Type type, out AssetObject asset)
    {
        asset = null!;
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

    public void ExtractMeta(Span<AssetStoreMeta> span)
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


    internal void Reload<TAsset>(TAsset asset, ReloadAssetDel<TAsset> factory) where TAsset : AssetObject
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

    internal TAsset Register<TAsset, TDesc>(TDesc descriptor, LoadSimpleAssetDel<TAsset, TDesc> factory)
        where TAsset : AssetObject where TDesc : class, IAssetDescriptor
    {
        var id = MakeAssetId();
        var asset = factory(id, descriptor, this);
        AddAsset(id, asset, ReadOnlySpan<AssetFileSpec>.Empty);
        return asset;
    }

    internal TAsset RegisterWithFiles<TAsset, TDesc>(TDesc descriptor, bool isCoreAsset,
        LoadAssetDel<TAsset, TDesc> factory)
        where TAsset : AssetObject where TDesc : class, IAssetDescriptor
    {
        var id = MakeAssetId();
        var asset = factory(id, descriptor, isCoreAsset, out var fileSpecs);
        ArgumentNullException.ThrowIfNull(fileSpecs);
        AddAsset(id, asset, fileSpecs);
        return asset;
    }


    internal TAsset RegisterWithEmbedded<TAsset, TDesc>(
        TDesc descriptor,
        bool isCoreAsset,
        LoadAdvancedAssetDel<TAsset, TDesc> factory,
        Action<ReadOnlySpan<IAssetEmbeddedDescriptor>> enqueueEmbedded)
        where TAsset : AssetObject
        where TDesc : class, IAssetDescriptor
    {
        var id = MakeAssetId();
        var asset = factory(id, descriptor, isCoreAsset, enqueueEmbedded, out var fileSpecs);
        ArgumentNullException.ThrowIfNull(fileSpecs);

        AddAsset(id, asset, fileSpecs);
        return asset;
    }

    internal TAsset RegisterEmbedded<TAsset, TEmbedded>(
        TEmbedded embedded,
        LoadEmbeddedAssetDel<TAsset, TEmbedded> factory)
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
        AddAsset(id, asset, embedded.FileSpec);
        //Logger.LogString(LogScope.Assets, $"{asset.Name} - Embedded {typeof(TAsset).Name} loaded");
        return asset;
    }

    private void AddAsset<TAsset>(AssetId id, TAsset asset, ReadOnlySpan<AssetFileSpec> fileSpecs)
        where TAsset : AssetObject
    {
        ArgumentNullException.ThrowIfNull(asset);
        ArgumentOutOfRangeException.ThrowIfLessThanOrEqual(id.Value, 0);
        ArgumentOutOfRangeException.ThrowIfNotEqual(asset.RawId.Value, id.Value);

        asset.GId = Guid.NewGuid();
        if (!_assets.TryAdd(id, asset))
            throw new InvalidOperationException($"Asset '{asset.Name}' is already registered by id.");

        if (!_names.TryAdd(new AssetKey(typeof(TAsset), asset.Name), id))
            throw new InvalidOperationException($"Asset '{asset.Name}' is already registered by type/name.");
        
        GetAssetList<TAsset>().Assets.Add(asset);

        IncrementTypeCount<TAsset>(fileSpecs.Length, asset.Kind);

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
            var spec = fileSpecs[i];
            var file = prevFiles[i];
            _files[file.Id] = new AssetFileEntry(file.Id, spec);
        }
    }

    private void IncrementTypeCount<TAsset>(int files, AssetKind kind) where TAsset : AssetObject
    {
        if (!_typeMeta.TryGetValue(typeof(TAsset), out var meta))
            _typeMeta[typeof(TAsset)] = meta = new AssetStoreTypeMeta(typeof(TAsset), kind);

        meta.Increment(files);
    }

    private readonly record struct AssetKey(Type RegistryType, string Name)
    {
        public static implicit operator AssetKey((Type, string ) k) => new(k.Item1, k.Item2);
        public static AssetKey For<T>(string name) where T : AssetObject => new(typeof(T), name);
    }
}