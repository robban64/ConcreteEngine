using ConcreteEngine.Core.Common;
using ConcreteEngine.Core.Common.Collections;
using ConcreteEngine.Core.Common.Memory;
using ConcreteEngine.Core.Engine.Assets;
using ConcreteEngine.Engine.Assets.Data;
using ConcreteEngine.Engine.Assets.Descriptors;
using ConcreteEngine.Engine.Assets.Loader.Data;
using ConcreteEngine.Engine.Assets.Utils;

namespace ConcreteEngine.Engine.Assets;

public sealed partial class AssetStore : IAssetChangeNotifier
{
    private const int DefaultCap = 512;
    public static int StoreCount => EnumCache<AssetKind>.Count - 1;

    private AssetId MakeAssetId() => new(_assets.AllocateNext() + 1);

    private readonly SlotArray<AssetObject> _assets = new(DefaultCap);
    private readonly AssetTypeCollection[] _collections;

    private readonly Dictionary<Guid, AssetId> _byGid = new(DefaultCap);
    private readonly Dictionary<(Type, string), AssetId> _byName = new(DefaultCap);

    private readonly AssetFileRegistry _fileRegistry;

    private readonly Func<string, Type, bool> _nameExistsDel;

    //
    public int Count => _assets.Count;
    public int Capacity => _assets.Capacity;
    internal IReadOnlyList<AssetTypeCollection> Collections => _collections;
    //

    internal AssetStore(AssetFileRegistry fileRegistry)
    {
        _fileRegistry = fileRegistry;
        _collections = AssetTypeCollection.CreateAll();
        _nameExistsDel = (name, type) => !_byName.ContainsKey((type, name));
    }

    internal void EnsureStoreCapacity(Queue<AssetRecord>[] queues)
    {
        GetAssetList<Shader>().EnsureCapacity(queues[AssetKind.Shader.ToIndex()].Count);
        GetAssetList<Model>().EnsureCapacity(queues[AssetKind.Model.ToIndex()].Count);
        GetAssetList<Texture>().EnsureCapacity(queues[AssetKind.Texture.ToIndex()].Count);
        GetAssetList<Material>().EnsureCapacity(queues[AssetKind.Material.ToIndex()].Count);
    }

    public void MarkDirty(AssetObject asset) => GetAssetList(asset.Kind).MarkDirty(asset.Id);

    public bool Has(AssetId assetId)
    {
        var index = assetId.Index();
        return (uint)index < (uint)_assets.Capacity && _assets[index]?.Id == assetId;
    }

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

        _fileRegistry.TryGetFileBindings(asset.Id, out var fileIds);
        var files = new AssetFileSpec[fileIds.Length];
        for (var i = 0; i < fileIds.Length; i++)
            files[i] = _fileRegistry.Get(fileIds[i]);

        factory(asset, files, out var fileSpecs);
        InvalidOpThrower.ThrowIf(gen != asset.Generation, nameof(asset.Generation));
        InvalidOpThrower.ThrowIf(files.Length != fileSpecs.Length, nameof(fileSpecs.Length));

        var newAsset = asset.CopyAndIncreaseGen();
        InvalidOpThrower.ThrowIf(newAsset.Generation != asset.Generation + 1, nameof(asset.Generation));

        _assets[asset.Id.Index()] = newAsset;
        if (fileSpecs.Length > 0) RegisterExistingBindings(asset.Id, fileSpecs);

        newAsset.AttachNotifier(this);
    }

    internal AssetId RegisterPlainAsset(Guid gid, AssetKind kind, string name, AssetStorageKind storageKind)
    {
        ArgumentException.ThrowIfNullOrEmpty(name);
        ArgumentOutOfRangeException.ThrowIfEqual(gid, Guid.Empty);

        var assetId = MakeAssetId();
        _byGid.Add(gid, assetId);

        var fileSpec = _fileRegistry.Add(assetId, name, name, 0, new FileScanInfo(0, kind, storageKind));
        GetAssetList(kind).AddFile(fileSpec);
        return assetId;
    }


    internal AssetId RegisterScannedAsset(AssetRecord record, string relativePath, in FileScanInfo fileInfo)
    {
        ArgumentException.ThrowIfNullOrEmpty(record.Name);
        ArgumentOutOfRangeException.ThrowIfEqual(record.GId, Guid.Empty);

        var assetType = AssetKindUtils.ToType(record.Kind);
        if (_byName.ContainsKey((assetType, record.Name)))
            throw new InvalidOperationException($"Asset name {record.Name} already registered");

        var assetId = MakeAssetId();
        _byGid.Add(record.GId, assetId);

        var fileSpec = _fileRegistry.Add(assetId, record.Name, relativePath, record.Files.Count, in fileInfo);
        GetAssetList(record.Kind).AddFile(fileSpec);
        return assetId;
    }


    internal void RegisterUnboundFile(string filename, string relativePath, in FileScanInfo scanInfo)
    {
        ArgumentException.ThrowIfNullOrEmpty(filename);
        ArgumentException.ThrowIfNullOrEmpty(relativePath);

        _fileRegistry.AddUnbound(filename, relativePath, in scanInfo);
    }

    internal void RegisterAssetBinding(AssetId assetId, string assetName, string relativePath, in FileScanInfo scanInfo)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(assetId.Value);
        ArgumentException.ThrowIfNullOrEmpty(assetName);
        ArgumentException.ThrowIfNullOrEmpty(relativePath);

        if (Has(assetId))
        {
            throw new InvalidOperationException(
                $"AssetId {assetId} not found for register scanned file {relativePath}");
        }

        if (!_fileRegistry.TryGetFileByPath(relativePath, out var fileSpec))
        {
            fileSpec = _fileRegistry.Add(AssetId.Empty, assetName, relativePath, 1, in scanInfo);
            GetAssetList(scanInfo.Kind).AddFile(fileSpec);
        }

        var fileIds = _fileRegistry.GetAssetFileBindings(assetId);
        if (fileIds[scanInfo.FileIndex].Value > 0)
            throw new InvalidOperationException($"FileSpec {scanInfo.FileIndex} already set for {assetName}");

        fileIds[scanInfo.FileIndex] = fileSpec.Id;
    }

    internal AssetId RegisterEmbedded(AssetId sourceId, IEmbeddedAsset embedded)
    {
        ArgumentNullException.ThrowIfNull(embedded);
        ArgumentNullException.ThrowIfNull(embedded.FileSpec);

        if (!_fileRegistry.HasBinding(sourceId))
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

        if (Has(asset.Id))
            throw new InvalidOperationException($"Asset '{asset.Name}:{asset.Id}' is already registered.");

        if (!_fileRegistry.TryGetFileBindings(asset.Id, out var fileBindings))
            throw new InvalidOperationException($"Asset '{asset.Name}:{asset.Id}' missing file bindings.");

        if (!_byName.TryAdd((typeof(TAsset), asset.Name), asset.Id))
        {
            var name = AssetNameUtils.IncrementName(asset.Name, typeof(TAsset), _nameExistsDel);
            asset.Name = name;
            _byName.Add((typeof(TAsset), asset.Name), asset.Id);
        }

        _assets[asset.Id.Index()] = asset;

        var assetList = GetAssetList<TAsset>();
        assetList.Add(asset);
        foreach (var binding in fileBindings)
        {
            assetList.AddFile(_fileRegistry.Get(binding));
        }

        asset.AttachNotifier(this);
    }


    private void RegisterExistingBindings(AssetId assetId, AssetFileSpec[] fileSpecs)
    {
        if (!_fileRegistry.TryGetFileBindings(assetId, out var bindings))
            throw new InvalidOperationException($"Missing file bindings for {assetId}");

        for (var i = 0; i < fileSpecs.Length; i++)
            _fileRegistry.Replace(bindings[i], fileSpecs[i]);
    }

}