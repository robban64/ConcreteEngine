using ConcreteEngine.Core.Common;
using ConcreteEngine.Core.Common.Memory;
using ConcreteEngine.Core.Engine.Assets;
using ConcreteEngine.Engine.Assets.Data;
using ConcreteEngine.Engine.Assets.Loader.Data;
using ConcreteEngine.Engine.Assets.Utils;

namespace ConcreteEngine.Engine.Assets;

public sealed partial class AssetStore : IAssetChangeNotifier
{
    public static int StoreCount => EnumCache<AssetKind>.Count - 1;

    private int _assetId;
    private int _assetFileId;
    private AssetId MakeAssetId() => new(++_assetId);
    private AssetFileId MakeAssetFileId() => new(++_assetFileId);

    private readonly AssetCollection[] _collections = new AssetCollection[EnumCache<AssetKind>.Count - 1];

    private readonly Dictionary<AssetId, AssetObject> _assets = [];
    private readonly Dictionary<Guid, AssetId> _byGid = [];
    private readonly Dictionary<AssetKey, AssetId> _byName = [];

    private readonly Dictionary<int, AssetFileSpec> _files = [];
    private readonly Dictionary<AssetId, AssetFileId[]> _fileBindings = [];

    public int Count => _assetId;
    public int FileCount => _files.Count;
    public int Capacity => _assets.Capacity;

    internal IReadOnlyList<AssetCollection> Collections => _collections;

    internal AssetStore()
    {
        if (_assetId > 0 || _assetFileId > 0) throw new InvalidOperationException();

        AssetCollection<Shader>.Create(_collections);
        AssetCollection<Model>.Create(_collections);
        AssetCollection<Texture>.Create(_collections);
        AssetCollection<Material>.Create(_collections);
    }

    internal void EnsureStoreCapacity(int assetCount, int shaderCount, int texCount, int modelCount, int matCount)
    {
        var count = int.Min(assetCount, 64);
        _assets.EnsureCapacity(count);
        _byGid.EnsureCapacity(count);
        _byName.EnsureCapacity(count);
        _files.EnsureCapacity(count);
        _fileBindings.EnsureCapacity(count);

        GetAssetList<Shader>().EnsureCapacity(int.Min(shaderCount, 16));
        GetAssetList<Model>().EnsureCapacity(int.Min(modelCount, 16));
        GetAssetList<Texture>().EnsureCapacity(int.Min(texCount, 16));
        GetAssetList<Material>().EnsureCapacity(int.Min(matCount, 16));
    }

    public void MarkDirty(AssetObject asset) => GetAssetList(asset.Kind).MarkDirty(asset.Id);

    public void Rename(AssetObject asset, string newName, Action<string> onSuccess)
    {
        if (asset.Name == newName) throw new ArgumentException("Rename: Identical name", nameof(newName));
        var type = AssetKindUtils.ToType(asset.Kind);
        if (_byName.ContainsKey((type, newName)))
            throw new ArgumentException("Rename: name already exists", nameof(newName));

        _byName.Remove((type, asset.Name));
        _byName.Add((type, newName), asset.Id);
        onSuccess(newName);
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

        var newAsset = asset.CopyAndIncreaseGen();
        InvalidOpThrower.ThrowIf(newAsset.Generation != asset.Generation + 1, nameof(asset.Generation));

        _assets[asset.Id] = newAsset;
        if (fileSpecs.Length > 0) RegisterExistingBindings(asset.Id, fileSpecs);

        newAsset.AttachNotifier(this);
    }

    internal AssetId RegisterScannedAsset(Guid gid, int fileCount)
    {
        if (gid == Guid.Empty) throw new ArgumentException(nameof(gid));

        var id = MakeAssetId();
        _byGid.Add(gid, id);
        _assets.Add(id, null!);
        _fileBindings.Add(id, fileCount == 0 ? [] : new AssetFileId[fileCount]);
        return id;
    }

    internal void RegisterScannedSpec(AssetId assetId, string assetName, string path, in FileScanInfo scanInfo)
    {
        ArgumentException.ThrowIfNullOrEmpty(assetName);
        ArgumentException.ThrowIfNullOrEmpty(path);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(assetId.Value);

        if (!_assets.ContainsKey(assetId))
            throw new InvalidOperationException($"AssetId {assetId} not found for register scanned file {path}");

        var spec = new AssetFileSpec(
            Id: MakeAssetFileId(),
            GId: Guid.NewGuid(),
            LogicalName: assetName,
            RelativePath: path,
            Storage: scanInfo.StorageKind,
            SizeBytes: scanInfo.SizeBytes,
            LastWriteTime: scanInfo.LastWriteTime,
            ContentHash: scanInfo.ContentHash,
            Source: scanInfo.Source
        );

        _files.Add(spec.Id, spec);

        var fileIds = _fileBindings[assetId];
        if (fileIds[scanInfo.FileIndex].Value > 0)
            throw new InvalidOperationException($"FileSpec {scanInfo.FileIndex} already set for {assetName}");

        fileIds[scanInfo.FileIndex] = spec.Id;
    }

    internal AssetId RegisterEmbedded(AssetId originalAssetId, IEmbeddedAsset embedded)
    {
        ArgumentNullException.ThrowIfNull(embedded);
        ArgumentNullException.ThrowIfNull(embedded.FileSpec);

        if (!_assets.ContainsKey(originalAssetId))
            throw new InvalidOperationException($"Missing original asset for {embedded.Name}");

        var assetId = RegisterScannedAsset(embedded.GId, 1);
        RegisterExistingBindings(assetId, [embedded.FileSpec]);
        return assetId;
    }


    public void AddAsset<TAsset>(TAsset asset) where TAsset : AssetObject
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

        _assets[asset.Id] = asset;
        GetAssetList<TAsset>().Add(asset, _fileBindings[asset.Id].Length);

        asset.AttachNotifier(this);
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