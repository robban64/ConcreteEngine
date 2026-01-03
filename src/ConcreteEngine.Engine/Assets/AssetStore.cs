using System.Runtime.InteropServices;
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



public sealed partial class AssetStore
{
    private const int DefaultCap = 256;

    private static int _assetId;
    private static int _assetFileId;
    private static AssetId MakeAssetId() => new(++_assetId);
    private static AssetFileId MakeAssetFileId() => new(++_assetFileId);

    private readonly IAssetList[] _assetLists = new IAssetList[EnumCache<AssetKind>.Count - 1];

    private readonly Dictionary<AssetId, AssetObject> _assets = new(DefaultCap);
    private readonly Dictionary<Guid, AssetId> _byGid = new(DefaultCap);
    private readonly Dictionary<AssetKey, AssetId> _byName = new(DefaultCap);

    private readonly Dictionary<AssetFileId, AssetFileEntry> _files = new(DefaultCap);
    private readonly Dictionary<AssetId, AssetFileId[]> _fileBindings = new(DefaultCap);

    private readonly Dictionary<Guid, AssetId> _embedded = new(8);

    public int Count => _assetId;
    public int FileCount => _files.Count;
    public int Capacity => _assets.Capacity;
    public int StoreCount => EnumCache<AssetKind>.Count - 1;
    
    internal IReadOnlyList<IAssetList> AssetLists => _assetLists;

    internal AssetStore()
    {
        if (_assetId > 0 || _assetFileId > 0) throw new InvalidOperationException();

        AssetList<Shader>.Create(_assetLists, 32);
        AssetList<Model>.Create(_assetLists, 32);
        AssetList<Texture2D>.Create(_assetLists, 64);
        AssetList<CubeMap>.Create(_assetLists, 4);
        AssetList<MaterialTemplate>.Create(_assetLists, 64);
    }


    internal void Reload<TAsset>(TAsset asset, ReloadAssetDel<TAsset> factory) where TAsset : AssetObject
    {
        var gen = asset.Generation;

        TryGetFileIds(asset.Id, out var fileIds);
        var files = new AssetFileEntry[fileIds.Length];
        for (var i = 0; i < fileIds.Length; i++)
            files[i] = _files[fileIds[i]];

        factory(asset, files, out var fileSpecs);
        InvalidOpThrower.ThrowIf(gen != asset.Generation, nameof(asset.Generation));
        InvalidOpThrower.ThrowIf(files.Length != fileSpecs.Length, nameof(fileSpecs.Length));

        asset.BumpGeneration();
        if (fileSpecs.Length > 0) RegisterExistingBindings(asset.Id, files, fileSpecs);
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

        if (_embedded.ContainsKey(embedded.GId))
            throw new InvalidOperationException($"Embedded resource is already registered. {embedded.GId}");

        var id = MakeAssetId();
        var asset = factory(id, embedded, this);
        _embedded.Add(embedded.GId, id);
        asset.Name = embedded.AssetName;
        asset.IsEmbedded = true;
        asset.GId = embedded.GId;
        AddAsset(id, asset, embedded.FileSpec);
        //Logger.LogString(LogScope.Assets, $"{asset.Name} - Embedded {typeof(TAsset).Name} loaded");
        return asset;
    }

    private void AddAsset<TAsset>(AssetId id, TAsset asset, ReadOnlySpan<AssetFileSpec> fileSpecs)
        where TAsset : AssetObject
    {
        ArgumentNullException.ThrowIfNull(asset);
        ArgumentOutOfRangeException.ThrowIfLessThanOrEqual(id.Value, 0);
        ArgumentOutOfRangeException.ThrowIfNotEqual(asset.Id.Value, id.Value);

        if (!_assets.TryAdd(id, asset))
            throw new InvalidOperationException($"Asset '{asset.Name}' is already registered by id.");

        if (!_byName.TryAdd(new AssetKey(typeof(TAsset), asset.Name), id))
            throw new InvalidOperationException($"Asset '{asset.Name}' is already registered by type/name.");

        if (asset.IsEmbedded && asset.GId == Guid.Empty)
            throw new InvalidOperationException($"Embedded asset missing GID");
        
        if(!asset.IsEmbedded) asset.GId = Guid.NewGuid();
        _byGid.Add(asset.GId, id);

        GetAssetList<TAsset>().Add(asset, fileSpecs.Length);

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

        _fileBindings.Add(assetId, fileIds);
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

    private readonly record struct AssetKey(Type RegistryType, string Name)
    {
        public static implicit operator AssetKey((Type, string ) k) => new(k.Item1, k.Item2);
        public static AssetKey For<T>(string name) where T : AssetObject => new(typeof(T), name);
    }
}