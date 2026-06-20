using System.Runtime.CompilerServices;
using ConcreteEngine.Core.Common;
using ConcreteEngine.Core.Engine.Assets.Descriptors;
using ConcreteEngine.Core.Engine.Assets.Utils;
using ConcreteEngine.Core.Engine.Configuration;
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
    public AssetFile GetAssetRootFile(AssetId id) => Files.Get(Store.GetAssetBinding(id, 0));

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public AssetFile GetAssetFile(AssetId id, int fileIndex) => Files.Get(Store.GetAssetBinding(id, fileIndex));

    public void Rename(AssetObject asset, string newName)
    {
        ArgumentOutOfRangeException.ThrowIfEqual(newName, asset.Name);
        AssetNameUtils.ValidateAssetName(newName);
        AssetStore.GetTypeStore(asset.Kind).Rename(asset.Name, newName);
    }

    internal AssetId RegisterInMemoryAsset(Guid gid, AssetKind kind, string name)
    {
        var assetId = Store.Register(gid, 0);
        var file = Files.RegisterRoot(assetId, name, new FileScanInfo( string.Empty, name));
        Store.SetAssetBinding(assetId, file.Id, 0);
        return assetId;
    }

    internal AssetId RegisterScannedAsset(AssetRecord record, in FileScanInfo fileInfo)
    {
        ArgumentException.ThrowIfNullOrEmpty(record.Name);

        if (Store.HasName(record.Kind, record.Name))
            Throwers.InvalidArgument($"Asset name {record.Name} already registered");

        var assetId = Store.Register(record.Id, record.FileCount);
        var file = Files.RegisterRoot(assetId, record.Name, in fileInfo);
        Store.SetAssetBinding(assetId, file.Id, 0); // root
        return assetId;
    }

    internal void RegisterAssetBinding(int fileIndex, AssetId assetId, AssetKind kind, string relativePath)
    {
        if (!Store.HasBinding(assetId)) Throwers.NotFoundBy(nameof(AssetId), assetId);
        if (kind == AssetKind.Shader) return;
        
        if (!Files.TryGetFileByPath(relativePath, out var file))
            Throwers.InvalidArgument(nameof(relativePath),$"Invalid file path {relativePath}");

        file.Binding = FileBinding.DependentFile;
        Store.SetAssetBinding(assetId, file.Id, fileIndex);
    }

    internal AssetId RegisterEmbedded(AssetId sourceId, IEmbeddedAsset embedded)
    {
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
        => new(Assets.GetAllAssetBindings(assetId), FileRegistry);

}