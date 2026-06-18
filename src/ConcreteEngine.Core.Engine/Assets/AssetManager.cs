using System.Runtime.CompilerServices;
using ConcreteEngine.Core.Common;
using ConcreteEngine.Core.Engine.Assets.Descriptors;
using ConcreteEngine.Core.Engine.Assets.Utils;
using ConcreteEngine.Graphics.Gfx;
using ConcreteEngine.Renderer.Buffer;
using ConcreteEngine.Renderer.Core;

namespace ConcreteEngine.Core.Engine.Assets;

public sealed class AssetManager
{
    public static AssetStore AssetStore
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => Instance.Store;
    }

    public static AssetFileRegistry FileRegistry
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => Instance.Files;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static MaterialProfile GetMaterialProfile(MaterialProfileId profileId) =>
        Instance._profileEntries[(int)profileId];

    //
    public static readonly AssetManager Instance = new();
    //

    public readonly AssetStore Store;
    public readonly AssetFileRegistry Files;

    private readonly MaterialProfile[] _profileEntries;

    private AssetManager()
    {
        Files = new AssetFileRegistry();
        Store = new AssetStore(Files);
        Store.SetupStores();

        _profileEntries = MaterialProfile.CreateProfiles();
    }
    
    public void Rename(AssetObject asset, string newName)
    {
        ArgumentOutOfRangeException.ThrowIfEqual(newName, asset.Name);
        AssetNameUtils.ValidateAssetName(newName);
        Store.GetTypeStore(asset.Kind).Rename(asset.Name, newName);
    }
    
    internal AssetId RegisterPlainAsset(Guid gid, AssetKind kind, string name, AssetStorage storage)
    {
        ArgumentException.ThrowIfNullOrEmpty(name);
        ArgumentOutOfRangeException.ThrowIfEqual(gid, Guid.Empty);
        ArgumentOutOfRangeException.ThrowIfEqual((int)storage, (int)AssetStorage.FileSystem);

        var assetId = Store.AllocateSlot(gid);
        Files.Add(assetId, name, name, 0, new FileScanInfo(0, kind, storage));
        return assetId;
    }
    
    internal AssetId RegisterScannedAsset(AssetRecord record, string relativePath, in FileScanInfo fileInfo)
    {
        ArgumentException.ThrowIfNullOrEmpty(record.Name);
        ArgumentOutOfRangeException.ThrowIfEqual(record.GId, Guid.Empty);

        if (Store.GetTypeStore(record.Kind).HasName(record.Name))
            throw new InvalidOperationException($"Asset name {record.Name} already registered");

        var assetId = Store.AllocateSlot(record.GId);
        Files.Add(assetId, record.Name, relativePath, record.Files.Count, in fileInfo);
        return assetId;
    }
    
    
    internal void RegisterAssetBinding(AssetId assetId, string assetName, string relativePath, in FileScanInfo scanInfo)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(assetId.Value);
        ArgumentException.ThrowIfNullOrEmpty(assetName);
        ArgumentException.ThrowIfNullOrEmpty(relativePath);

        if (Store.Has(assetId))
            Throwers.InvalidOperation($"AssetId {assetId} not found for register scanned file {relativePath}");

        var name = Path.GetFileNameWithoutExtension(relativePath);
        if (!Files.TryGetFileByPath(relativePath, out var fileSpec))
            fileSpec = Files.Add(AssetId.Empty, name, relativePath, 1, in scanInfo);

        var fileIds = Files.GetFileBindings(assetId);
        if (fileIds[scanInfo.FileIndex].Value > 0)
            throw new InvalidOperationException($"FileSpec {name} already set for {assetName}");

        fileIds[scanInfo.FileIndex] = fileSpec.Id;
    }
    
    
    internal AssetId RegisterEmbedded(AssetId sourceId, IEmbeddedAsset embedded)
    {
        ArgumentNullException.ThrowIfNull(embedded);
        ArgumentNullException.ThrowIfNull(embedded.FileSpec);

        if (!Files.HasBinding(sourceId))
            throw new InvalidOperationException($"Missing original asset for {embedded.Name}");

        var assetId = RegisterPlainAsset(embedded.GId, embedded.Kind, embedded.Name, AssetStorage.Embedded);
        RegisterExistingBindings(assetId, [embedded.FileSpec]);
        return assetId;
    }
    
    internal void RegisterExistingBindings(AssetId assetId, AssetFile[] fileSpecs)
    {
        if (!Files.TryGetFileBindings(assetId, out var bindings))
            throw new InvalidOperationException($"Missing file bindings for {assetId}");

        for (var i = 0; i < fileSpecs.Length; i++)
            Files.Replace(bindings[i], fileSpecs[i]);
    }


    internal void AttachShaders()
    {
        AssetStore.Core.SetupShaders(Store);

        foreach (var entry in _profileEntries)
        {
            if (entry.Shader != null!) continue;
            entry.AttachShader(Store.GetByName<Shader>(entry.ShaderName));
        }

        AssetStore.Core.CreateMaterials(this);
    }
}
