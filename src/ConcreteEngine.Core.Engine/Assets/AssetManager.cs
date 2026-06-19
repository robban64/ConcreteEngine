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
    public static AssetStore Assets
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
        Store = new AssetStore();
        Store.SetupStores();

        _profileEntries = MaterialProfile.CreateProfiles();
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public AssetFile GetAssetRootFile(AssetId id) => Files.Get(Store.GetFileBindings(id)[0]);

    public void Rename(AssetObject asset, string newName)
    {
        ArgumentOutOfRangeException.ThrowIfEqual(newName, asset.Name);
        AssetNameUtils.ValidateAssetName(newName);
        AssetStore.GetTypeStore(asset.Kind).Rename(asset.Name, newName);
    }

    internal AssetId RegisterInMemoryAsset(Guid gid, AssetKind kind, string name)
    {
        ArgumentException.ThrowIfNullOrEmpty(name);
        ArgumentOutOfRangeException.ThrowIfEqual(gid, Guid.Empty);

        var assetId = Store.Register(gid, 0);
        var file = Files.RegisterRoot(assetId, new FileScanInfo(0, name, name));
        Store.SetAssetBinding(assetId, file.Id, 0);
        return assetId;
    }

    internal AssetId RegisterScannedAsset(AssetRecord record, in FileScanInfo fileInfo)
    {
        ArgumentException.ThrowIfNullOrEmpty(record.Name);
        ArgumentOutOfRangeException.ThrowIfEqual(record.GId, Guid.Empty);

        if (AssetStore.GetTypeStore(record.Kind).HasName(record.Name))
            Throwers.InvalidArgument($"Asset name {record.Name} already registered");

        var assetId = Store.Register(record.GId, record.Files.Count);
        var file = Files.RegisterRoot(assetId, in fileInfo);
        Store.SetAssetBinding(assetId, file.Id, 0);
        return assetId;
    }

    internal void RegisterAssetBinding(AssetId assetId, in FileScanInfo scanInfo)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(assetId.Id);

        if (Store.Has(assetId))
            Throwers.InvalidArgument($"AssetId {assetId} not found for register scanned file {scanInfo.Name}");

        if (!Files.TryGetFileByPath(scanInfo.RelativePath, out var fileSpec))
            fileSpec = Files.RegisterFile(FileBinding.DependentFile,in scanInfo);

        Store.SetAssetBinding(assetId, fileSpec.Id, scanInfo.FileIndex);
    }


    internal AssetId RegisterEmbedded(AssetId sourceId, IEmbeddedAsset embedded)
    {
        ArgumentNullException.ThrowIfNull(embedded);
        ArgumentNullException.ThrowIfNull(embedded.FileSpec);

        if (!Store.HasBinding(sourceId))
            Throwers.InvalidArgument($"Missing original asset for {embedded.Name}");

        var assetId = RegisterInMemoryAsset(embedded.GId, embedded.Kind, embedded.Name);
        RegisterExistingBindings(assetId, [embedded.FileSpec]);
        return assetId;
    }

    internal void RegisterExistingBindings(AssetId assetId, AssetFile[] fileSpecs)
    {
        if (!Store.TryGetFileBindings(assetId, out var bindings))
            Throwers.InvalidArgument($"Missing file bindings for {assetId}");

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
    
    public static AssetBindingEnumerator GetAssetBindingsEnumerator(AssetId assetId) 
        => new(Assets.GetFileBindings(assetId), FileRegistry);

}