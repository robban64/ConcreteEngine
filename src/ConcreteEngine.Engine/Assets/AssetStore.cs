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

        AssetList<Shader>.Create(_assetLists);
        AssetList<Model>.Create(_assetLists);
        AssetList<Texture2D>.Create(_assetLists);
        AssetList<MaterialTemplate>.Create(_assetLists);
        
        EnsureStoreCapacity(256, 16, 32, 32, 32);
    }

    internal void EnsureStoreCapacity(int assetCount, int shaderCount,int texCount, int modelCount, int matCount)
    {
        _assets.EnsureCapacity(assetCount);
        _byGid.EnsureCapacity(assetCount);
        _byName.EnsureCapacity(assetCount);
        _files.EnsureCapacity(assetCount);
        _fileBindings.EnsureCapacity(assetCount);

        GetAssetList<Shader>().EnsureCapacity(shaderCount);
        GetAssetList<Model>().EnsureCapacity(modelCount);
        GetAssetList<Texture2D>().EnsureCapacity(texCount);
        GetAssetList<MaterialTemplate>().EnsureCapacity(matCount);
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

    internal AssetId RegisterScannedAsset(int fileCount)
    {
        var assetArg = MakeAssetArg();
        _byGid.Add(assetArg.GId, assetArg.Id);
        _assets.Add(assetArg.Id, null!);
        _fileBindings.Add(assetArg.Id, new AssetFileId[fileCount]);
        return assetArg.Id;
    }

    internal void RegisterScannedSpec(AssetId assetId, string assetName, string path, in FileScanInfo scanInfo)
    {
        ArgumentException.ThrowIfNullOrEmpty(assetName);
        ArgumentException.ThrowIfNullOrEmpty(path);
        
        if (!_assets.ContainsKey(assetId))
            throw new InvalidOperationException($"AssetId {assetId} not found for register scanned file {path}");

        var spec = new AssetFileSpec(
            Id: MakeAssetFileId(),
            GId: Guid.NewGuid(),
            LogicalName: assetName,
            RelativePath: path,
            Storage: scanInfo.StorageKind,
            SizeBytes: scanInfo.SizeBytes,
            ContentHash: scanInfo.ContentHash,
            Source: scanInfo.Source
        );

        _files.Add(spec.Id, spec);
        
        
        var fileIds = _fileBindings[assetId];
        if (fileIds[scanInfo.FileIndex].Value > 0)
            throw new InvalidOperationException($"FileSpec {scanInfo.FileIndex} already set for {assetName}");

        fileIds[scanInfo.FileIndex] = spec.Id;
    }


    private void AddAsset<TAsset>(TAsset asset) where TAsset : AssetObject
    {
        ArgumentNullException.ThrowIfNull(asset);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(asset.Id.Value);

        if (asset.GId == Guid.Empty) throw new ArgumentException(nameof(asset.GId));

        if (!_assets.ContainsKey(asset.Id))
            throw new InvalidOperationException($"Asset '{asset.Name}:{asset.Id}' is not registered by id.");

        if (!_fileBindings.ContainsKey(asset.Id))
            throw new InvalidOperationException($"Asset '{asset.Name}:{asset.Id}' missing file bindings.");

        if (!_byName.TryAdd(new AssetKey(typeof(TAsset), asset.Name), asset.Id))
            throw new InvalidOperationException($"Asset '{asset.Name}:{asset.Id}' is already registered by type/name.");


        GetAssetList<TAsset>().Add(asset, _fileBindings[asset.Id].Length);
    }


    internal void RegisterEmbedded<TAsset, TEmbedded>(AssetId originalAssetId, TEmbedded embedded)
        where TAsset : AssetObject where TEmbedded : EmbeddedRecord
    {
        ArgumentNullException.ThrowIfNull(embedded);
        ArgumentNullException.ThrowIfNull(embedded.FileSpec);
        ArgumentOutOfRangeException.ThrowIfZero(embedded.FileSpec.Length);
        InvalidOpThrower.ThrowIf(embedded.GId == Guid.Empty);

        if (!TryGet(originalAssetId, out var originalAsset))
            throw new InvalidOperationException($"Missing original asset for {embedded.AssetName}");
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