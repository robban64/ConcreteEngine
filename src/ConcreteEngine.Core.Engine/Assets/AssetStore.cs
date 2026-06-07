using System.Runtime.CompilerServices;
using ConcreteEngine.Core.Common;
using ConcreteEngine.Core.Common.Collections;
using ConcreteEngine.Core.Diagnostics.Logging;
using ConcreteEngine.Core.Engine.Assets.Descriptors;
using ConcreteEngine.Core.Engine.Assets.Utils;

namespace ConcreteEngine.Core.Engine.Assets;

public sealed partial class AssetStore
{
    private const int DefaultCap = 512;
    public static int StoreCount => EnumCache<AssetKind>.Count - 1;

    public static readonly AssetStore Instance = new();

    private static readonly Func<string, Type, bool> NameExistsDel =
        static (name, type) => !Instance.GetTypeStore(AssetKindUtils.ToAssetKind(type)).HasName(name);

    public int Count { get; private set; }

    public readonly AssetFileRegistry FileRegistry;

    private AssetObject?[] _assets = new AssetObject?[DefaultCap];
    private readonly Dictionary<Guid, AssetId> _byGid = new(DefaultCap);

    private readonly AssetTypeStore[] _collections;

    private readonly Stack<int> _free = [];

    private AssetStore()
    {
        FileRegistry = new AssetFileRegistry();
        _collections = AssetTypeStore.CreateAll();
    }

    //

    public int FreeCount => _free.Count;
    public int ActiveCount => Count - _free.Count;
    public int Capacity => _assets.Length;
    internal IReadOnlyList<AssetTypeStore> Collections => _collections;
    //

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public AssetTypeStore GetTypeStore(AssetKind kind) => _collections[kind.ToIndex()];

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public AssetTypeStore GetTypeStore(Type type) => _collections[AssetKindUtils.ToAssetKind(type).ToIndex()];


    public void MarkDirty(AssetObject asset) => GetTypeStore(asset.Kind).MarkDirty(asset);

    public void Rename(AssetObject asset, string newName)
    {
        AssetNameUtils.ValidateAssetName(newName);
        if (asset.Name == newName)
            throw new ArgumentException("Rename: Identical name", nameof(newName));

        GetTypeStore(asset.Kind).Rename(asset.Name, newName);
    }

    private AssetId AllocateSlot()
    {
        var freeIndex = SlotHelper.NextSlot(_free, Count);
        if (freeIndex >= 0) return new AssetId(freeIndex + 1, 1);

        if (SlotHelper.EnsureCapacity(ref _assets, Count, 1, out var oldSize))
            Logger.Log(StringLogEvent.MakeResize(LogScope.Assets, nameof(AssetStore), oldSize, _assets.Length));

        return new AssetId(++Count, 1);
    }


    internal AssetId RegisterPlainAsset(Guid gid, AssetKind kind, string name, AssetStorageKind storageKind)
    {
        ArgumentException.ThrowIfNullOrEmpty(name);
        ArgumentOutOfRangeException.ThrowIfEqual(gid, Guid.Empty);
        ArgumentOutOfRangeException.ThrowIfEqual((int)storageKind, (int)AssetStorageKind.FileSystem);

        var assetId = AllocateSlot();
        _byGid.Add(gid, assetId);
        FileRegistry.Add(assetId, name, name, 0, new FileScanInfo(0, kind, storageKind));
        return assetId;
    }


    internal AssetId RegisterScannedAsset(AssetRecord record, string relativePath, in FileScanInfo fileInfo)
    {
        ArgumentException.ThrowIfNullOrEmpty(record.Name);
        ArgumentOutOfRangeException.ThrowIfEqual(record.GId, Guid.Empty);

        if (GetTypeStore(record.Kind).HasName(record.Name))
            throw new InvalidOperationException($"Asset name {record.Name} already registered");

        var assetId = AllocateSlot();
        _byGid.Add(record.GId, assetId);
        FileRegistry.Add(assetId, record.Name, relativePath, record.Files.Count, in fileInfo);
        return assetId;
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

        var name = Path.GetFileNameWithoutExtension(relativePath);
        if (!FileRegistry.TryGetFileByPath(relativePath, out var fileSpec))
            fileSpec = FileRegistry.Add(AssetId.Empty, name, relativePath, 1, in scanInfo);

        var fileIds = FileRegistry.GetFileBindings(assetId);
        if (fileIds[scanInfo.FileIndex].Value > 0)
            throw new InvalidOperationException($"FileSpec {name} already set for {assetName}");

        fileIds[scanInfo.FileIndex] = fileSpec.Id;
    }

    internal AssetId RegisterEmbedded(AssetId sourceId, IEmbeddedAsset embedded)
    {
        ArgumentNullException.ThrowIfNull(embedded);
        ArgumentNullException.ThrowIfNull(embedded.FileSpec);

        if (!FileRegistry.HasBinding(sourceId))
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

        if (!FileRegistry.TryGetFileBindings(asset.Id, out _))
            throw new InvalidOperationException($"Asset '{asset.Name}:{asset.Id}' missing file bindings.");

        var assetList = GetTypeStore(asset.Kind);

        var name = asset.Name;

        if (assetList.HasName(name))
        {
            name = AssetNameUtils.IncrementName(name, typeof(TAsset), NameExistsDel);
            asset.Name = name;
        }

        _assets[asset.Id.Index()] = asset;

        assetList.Add(asset);
        MarkDirty(asset);
    }

    public Material CreateMaterial(string materialName, string newName)
    {
        ArgumentException.ThrowIfNullOrEmpty(materialName);
        ArgumentException.ThrowIfNullOrEmpty(newName);

        var originalMaterial = GetByName<Material>(materialName);

        var gid = Guid.NewGuid();
        var assetId = RegisterPlainAsset(gid, AssetKind.Material, newName, AssetStorageKind.InMemory);
        var material = originalMaterial.MakeNewAsTemplate(assetId, gid, newName);
        AddAsset(material);
        return material;
    }


    internal void RegisterExistingBindings(AssetId assetId, AssetFile[] fileSpecs)
    {
        if (!FileRegistry.TryGetFileBindings(assetId, out var bindings))
            throw new InvalidOperationException($"Missing file bindings for {assetId}");

        for (var i = 0; i < fileSpecs.Length; i++)
            FileRegistry.Replace(bindings[i], fileSpecs[i]);
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    internal void EnsureStoreCapacity(Queue<AssetRecord>[] queues)
    {
        GetTypeStore(AssetKind.Shader).EnsureCapacity(queues[AssetKind.Shader.ToIndex()].Count);
        GetTypeStore(AssetKind.Model).EnsureCapacity(queues[AssetKind.Model.ToIndex()].Count);
        GetTypeStore(AssetKind.Texture).EnsureCapacity(queues[AssetKind.Texture.ToIndex()].Count);
        GetTypeStore(AssetKind.Material).EnsureCapacity(queues[AssetKind.Material.ToIndex()].Count);
    }
}