using System.Runtime.InteropServices;
using ConcreteEngine.Core.Common;
using ConcreteEngine.Core.Common.Memory;
using ConcreteEngine.Engine.Assets.Data;
using ConcreteEngine.Engine.Assets.Descriptors;
using ConcreteEngine.Engine.Assets.Internal;
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
    private static AssetIdArgs MakeAssetArg() => new(MakeAssetId(), Guid.NewGuid());
    private static FileSpecArgs MakeFileSpecArg() => new(MakeAssetFileId(), Guid.NewGuid());


    private readonly IAssetList[] _assetLists = new IAssetList[EnumCache<AssetKind>.Count - 1];

    private readonly Dictionary<AssetId, AssetObject> _assets = new(DefaultCap);
    private readonly Dictionary<Guid, AssetId> _byGid = new(DefaultCap);
    private readonly Dictionary<AssetKey, AssetId> _byName = new(DefaultCap);

    private readonly Dictionary<AssetFileId, AssetFileSpec> _files = new(DefaultCap);
    private readonly Dictionary<AssetId, AssetFileId[]> _fileBindings = new(DefaultCap);


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
        var files = new AssetFileSpec[fileIds.Length];
        for (var i = 0; i < fileIds.Length; i++)
            files[i] = _files[fileIds[i]];

        factory(asset, files, out var fileSpecs);
        InvalidOpThrower.ThrowIf(gen != asset.Generation, nameof(asset.Generation));
        InvalidOpThrower.ThrowIf(files.Length != fileSpecs.Length, nameof(fileSpecs.Length));

        asset.BumpGeneration();
        if (fileSpecs.Length > 0) RegisterExistingBindings(asset.Id, fileSpecs);
    }

    internal TAsset Register<TAsset, TDesc>(TDesc descriptor, LoadSimpleAssetDel<TAsset, TDesc> factory)
        where TAsset : AssetObject where TDesc : class, IAssetDescriptor
    {
        var id = MakeAssetId();
        var asset = factory(id, descriptor, this);
        AddAsset(id, asset, []);
        return asset;
    }

    internal TAsset Register<TAsset, TDesc>(TDesc descriptor, bool isCore, out EmbeddedRecord[] embedded,
        LoadAssetDel<TAsset, TDesc> factory)
        where TAsset : AssetObject where TDesc : class, IAssetDescriptor
    {
        var id = MakeAssetId();
        var ctx = new LoadAssetContext(id, Guid.NewGuid(), isCore, MakeFileSpecArg);
        var asset = factory(descriptor, ref ctx);
        ArgumentNullException.ThrowIfNull(ctx.FileSpecs);

        if (ctx.EmbeddedSpan.IsEmpty) embedded = [];
        else embedded = ctx.EmbeddedSpan.ToArray();

        AddAsset(id, asset, ctx.FileSpecs);
        return asset;
    }


    internal TAsset RegisterEmbedded<TAsset, TEmbedded>(
        AssetId originalAssetId,
        TEmbedded embedded,
        LoadEmbeddedAssetDel<TAsset, TEmbedded> factory)
        where TAsset : AssetObject where TEmbedded : EmbeddedRecord
    {
        ArgumentNullException.ThrowIfNull(embedded);
        ArgumentNullException.ThrowIfNull(embedded.FileSpec);
        ArgumentOutOfRangeException.ThrowIfZero(embedded.FileSpec.Length);

        if (!TryGet(originalAssetId, out var originalAsset))
            throw new InvalidOperationException($"Missing original asset for {embedded.AssetName}");

        var id = MakeAssetId();
        var asset = factory(id, embedded, this);
        // if (asset.GId != embedded.GId)
        //    throw new InvalidOperationException("GId between embedded and asset doesnt match");
        asset.Name = embedded.AssetName;
        asset.IsEmbedded = true;
        AddAsset(id, asset, embedded.FileSpec);
        //Logger.LogString(LogScope.Assets, $"{asset.Name} - Embedded {typeof(TAsset).Name} loaded");
        return asset;
    }

    private void AddAsset<TAsset>(AssetId id, TAsset asset, AssetFileSpec[] fileSpecs)
        where TAsset : AssetObject
    {
        ArgumentNullException.ThrowIfNull(asset);
        ArgumentOutOfRangeException.ThrowIfLessThanOrEqual(id.Value, 0);
        ArgumentOutOfRangeException.ThrowIfNotEqual(asset.Id.Value, id.Value);

        if (asset.GId == Guid.Empty) throw new ArgumentException(nameof(asset.GId));

        if (!_assets.TryAdd(asset.Id, asset))
            throw new InvalidOperationException($"Asset '{asset.Name}' is already registered by id.");

        if (!_byName.TryAdd(new AssetKey(typeof(TAsset), asset.Name), asset.Id))
            throw new InvalidOperationException($"Asset '{asset.Name}' is already registered by type/name.");

        if (asset.GId == Guid.Empty)
            throw new InvalidOperationException($"Embedded asset missing GID");

        _byGid.Add(asset.GId, id);

        if (asset.IsEmbedded)
        {
            for (var i = 0; i < fileSpecs.Length; i++)
            {
                var fileSpec = fileSpecs[i];
                fileSpecs[i] = fileSpec with { Id = MakeAssetFileId(), LogicalName = asset.Name};
            }
        }

        GetAssetList<TAsset>().Add(asset, fileSpecs.Length);
        if (fileSpecs.Length > 0)
            RegisterNewBindings(asset.Id, fileSpecs);
    }

    private void RegisterNewBindings(AssetId assetId, AssetFileSpec[] fileSpecs)
    {
        var fileIds = new AssetFileId[fileSpecs.Length];

        for (var i = 0; i < fileSpecs.Length; i++)
        {
            var spec = fileSpecs[i];
            if (spec.GId == Guid.Empty) throw new InvalidOperationException(nameof(spec.GId));
            if (!spec.Id.IsValid()) throw new InvalidOperationException(nameof(spec.Id));
            if (string.IsNullOrEmpty(spec.LogicalName)) throw new InvalidOperationException(nameof(spec.LogicalName));
            if (string.IsNullOrEmpty(spec.RelativePath)) throw new InvalidOperationException(nameof(spec.RelativePath));

            var fileId = new AssetFileId(_assetFileId++);
            fileIds[i] = fileId;
            _files.Add(fileId, spec);
        }

        _fileBindings.Add(assetId, fileIds);
    }

    private void RegisterExistingBindings(AssetId assetId, AssetFileSpec[] fileSpecs)
    {
        var prevFileId = GetFileIds(assetId);
        for (var i = 0; i < fileSpecs.Length; i++)
        {
            var prevId = prevFileId[i];

            var spec = fileSpecs[i];
            _files[prevId] = spec;
        }
    }

    private readonly record struct AssetKey(Type RegistryType, string Name)
    {
        public static implicit operator AssetKey((Type, string ) k) => new(k.Item1, k.Item2);
        public static AssetKey For<T>(string name) where T : AssetObject => new(typeof(T), name);
    }
}