using ConcreteEngine.Core.Common;
using ConcreteEngine.Core.Common.Memory;
using ConcreteEngine.Core.Engine.Assets;
using ConcreteEngine.Engine.Assets.Data;
using ConcreteEngine.Engine.Assets.Descriptors;
using ConcreteEngine.Engine.Assets.Loader.Data;
using ConcreteEngine.Engine.Assets.Utils;

namespace ConcreteEngine.Engine.Assets;

public sealed partial class AssetStore : IAssetChangeNotifier
{
    public static int StoreCount => EnumCache<AssetKind>.Count - 1;
    private AssetId MakeAssetId() => new(++_assetId);
    private AssetFileId MakeAssetFileId() => new(++_assetFileId);

    private const int DefaultCap = 512;


    private int _assetId;
    private int _assetFileId;

    private readonly AssetCollection[] _collections = new AssetCollection[StoreCount];

    private readonly Dictionary<AssetId, AssetObject> _assets = new(DefaultCap);
    private readonly Dictionary<Guid, AssetId> _byGid = new(DefaultCap);
    private readonly Dictionary<AssetKey, AssetId> _byName = new(DefaultCap);

    private readonly Dictionary<int, AssetFileSpec> _files = new(DefaultCap);
    private readonly Dictionary<string, int> _fileByName = new(DefaultCap);

    private readonly Dictionary<AssetId, AssetFileId[]> _fileBindings = new(DefaultCap);
    private readonly HashSet<int> _pendingFiles = new(64);

    private readonly Func<string, Type, bool> _nameExistsDel;

    //
    public int Count => _assets.Count;
    public int FileCount => _files.Count;
    public int Capacity => _assets.Capacity;
    public int PendingFileCount => _pendingFiles.Count;
    internal IReadOnlyList<AssetCollection> Collections => _collections;

    //
    internal AssetStore()
    {
        if (_assetId > 0 || _assetFileId > 0) throw new InvalidOperationException();

        AssetCollection<Shader>.Create(_collections);
        AssetCollection<Model>.Create(_collections);
        AssetCollection<Texture>.Create(_collections);
        AssetCollection<Material>.Create(_collections);

        _nameExistsDel = (name, type) => !_byName.ContainsKey(new AssetKey(type, name));
    }

    internal void EnsureStoreCapacity(Queue<AssetRecord>[] queues)
    {
        GetAssetList<Shader>().EnsureCapacity(queues[AssetKind.Shader.ToIndex()].Count);
        GetAssetList<Model>().EnsureCapacity(queues[AssetKind.Model.ToIndex()].Count);
        GetAssetList<Texture>().EnsureCapacity(queues[AssetKind.Texture.ToIndex()].Count);
        GetAssetList<Material>().EnsureCapacity(queues[AssetKind.Material.ToIndex()].Count);
    }

    public void MarkDirty(AssetObject asset) => GetAssetList(asset.Kind).MarkDirty(asset.Id);

    public void Rename(AssetObject asset, string newName, Action<string> onSuccess)
    {
        AssetNameUtils.ValidateAssetName(newName);
        if (asset.Name == newName)
            throw new ArgumentException("Rename: Identical name", nameof(newName));

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

    internal AssetId RegisterPlainAsset(Guid gid, AssetKind kind, string name, AssetStorageKind storageKind)
    {
        ArgumentException.ThrowIfNullOrEmpty(name);
        ArgumentOutOfRangeException.ThrowIfEqual(gid, Guid.Empty);

        var assetId = MakeAssetId();
        var fileId = MakeAssetFileId();
        var fileSpec = MakeFileSpec(fileId, name, "InMemory", new FileScanInfo(0, kind, storageKind));

        _byGid.Add(gid, assetId);
        _assets.Add(assetId, null!);
        _files.Add(fileId, fileSpec);
        _fileBindings.Add(assetId, [fileId]);
        _fileByName.Add(name, fileId);
        return assetId;
    }


    internal AssetId RegisterScannedAsset(AssetRecord record, string relativePath, in FileScanInfo fileInfo)
    {
        ArgumentException.ThrowIfNullOrEmpty(record.Name);
        ArgumentOutOfRangeException.ThrowIfEqual(record.GId, Guid.Empty);

        var assetType = AssetKindUtils.ToType(record.Kind);
        if (_byName.ContainsKey(new AssetKey(assetType, record.Name)))
            throw new InvalidOperationException($"Asset name {record.Name} already registered");

        if (_fileByName.ContainsKey(relativePath))
            throw new InvalidOperationException($"AssetFile {relativePath} already registered");

        var assetId = MakeAssetId();
        var fileId = MakeAssetFileId();
        var fileSpec = MakeFileSpec(fileId, record.Name, relativePath, in fileInfo);
        var fileBindings = new AssetFileId[record.Files.Count + 1];
        fileBindings[0] = fileId;

        _byGid.Add(record.GId, assetId);
        _assets.Add(assetId, null!);

        _files.Add(fileId, fileSpec);
        _fileBindings.Add(assetId, fileBindings);
        _fileByName.Add(relativePath, fileId);
        return assetId;
    }


    internal void RegisterFile(string filename, string relativePath, in FileScanInfo scanInfo)
    {
        ArgumentException.ThrowIfNullOrEmpty(filename);
        ArgumentException.ThrowIfNullOrEmpty(relativePath);

        if (_fileByName.ContainsKey(relativePath))
            throw new InvalidOperationException($"AssetFile {relativePath} already registered");

        var fileId = MakeAssetFileId();
        var fileSpec = MakeFileSpec(fileId, filename, relativePath, in scanInfo);
        _files.Add(fileId, fileSpec);
        _pendingFiles.Add(fileId);
    }

    internal void RegisterAssetBinding(AssetId assetId, string assetName, string path, in FileScanInfo scanInfo)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(assetId.Value);
        ArgumentException.ThrowIfNullOrEmpty(assetName);
        ArgumentException.ThrowIfNullOrEmpty(path);

        if (!_assets.ContainsKey(assetId))
            throw new InvalidOperationException($"AssetId {assetId} not found for register scanned file {path}");

        var fileSpec = MakeFileSpec(MakeAssetFileId(), assetName, path, in scanInfo);

        _files.Add(fileSpec.Id, fileSpec);

        var fileIds = _fileBindings[assetId];
        if (fileIds[scanInfo.FileIndex].Value > 0)
            throw new InvalidOperationException($"FileSpec {scanInfo.FileIndex} already set for {assetName}");

        fileIds[scanInfo.FileIndex] = fileSpec.Id;
    }

    //TODO
    internal AssetId RegisterEmbedded(AssetId sourceId, IEmbeddedAsset embedded)
    {
        ArgumentNullException.ThrowIfNull(embedded);
        ArgumentNullException.ThrowIfNull(embedded.FileSpec);

        if (!_assets.ContainsKey(sourceId))
            throw new InvalidOperationException($"Missing original asset for {embedded.Name}");

        var assetId = RegisterPlainAsset(embedded.GId, embedded.Kind, embedded.Name, AssetStorageKind.Embedded);
        RegisterExistingBindings(assetId, [embedded.FileSpec]);
        return assetId;
    }


    public void AddAsset<TAsset>(TAsset asset) where TAsset : AssetObject
    {
        ArgumentNullException.ThrowIfNull(asset);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(asset.Id.Value);
        ArgumentOutOfRangeException.ThrowIfEqual(asset.GId, Guid.Empty);


        if (!_assets.ContainsKey(asset.Id))
            throw new InvalidOperationException($"Asset '{asset.Name}:{asset.Id}' is not registered by id.");

        if (!_fileBindings.TryGetValue(asset.Id, out var fileBindings))
            throw new InvalidOperationException($"Asset '{asset.Name}:{asset.Id}' missing file bindings.");

        if (!_byName.TryAdd(new AssetKey(typeof(TAsset), asset.Name), asset.Id))
        {
            var name = AssetNameUtils.IncrementName(asset.Name, typeof(TAsset), _nameExistsDel);
            asset.Name = name;
            _byName.Add(new AssetKey(typeof(TAsset), asset.Name), asset.Id);
        }

        _assets[asset.Id] = asset;

        var assetList = GetAssetList<TAsset>();
        assetList.Add(asset);
        foreach (var binding in fileBindings)
        {
            assetList.AddFile(_files[binding]);
        }

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

    private static AssetFileSpec MakeFileSpec(AssetFileId id, string name, string path, in FileScanInfo scanInfo)
    {
        return new AssetFileSpec(
            Id: id,
            GId: Guid.NewGuid(),
            LogicalName: name,
            RelativePath: path,
            Storage: scanInfo.StorageKind,
            SizeBytes: scanInfo.SizeBytes,
            LastWriteTime: scanInfo.LastWriteTime,
            ContentHash: null,
            Source: scanInfo.Source
        );
    }
}