using System.Runtime.CompilerServices;
using ConcreteEngine.Core.Common;
using ConcreteEngine.Core.Diagnostics.Logging;
using ConcreteEngine.Core.Engine.Assets;
using ConcreteEngine.Engine.Assets.Descriptors;
using ConcreteEngine.Engine.Assets.Loader;
using ConcreteEngine.Engine.Assets.Loader.Data;
using ConcreteEngine.Engine.Assets.Utils;
using ConcreteEngine.Engine.Configuration.IO;
using ConcreteEngine.Engine.Editor.Diagnostics;

namespace ConcreteEngine.Engine.Assets.Internal;

internal static class AssetPriority
{
    public const int Texture = 0;
    public const int Material = 10;
    public const int Mesh = 20;
}

internal sealed class AssetLoader
{
    private enum ProcessStepOrder
    {
        NotStarted,
        Shaders,
        Textures,
        Meshes,
        Materials,
        Finished
    }

    private AssetStore? _store;

    private AssetGfxUploader? _gfxUploader;

    public bool IsActive { get; private set; }

    private ProcessStepOrder _step;

    private Queue<AssetRecord>[]? _recordQueue;
    private readonly IAssetTypeLoader?[] _loaders = new IAssetTypeLoader[AssetKindUtils.AssetTypeCount];

    private LoaderContext MakeContext(AssetRecord record, string path, bool isHotReload = false)
    {
        _store!.TryGetIdByGuid(record.GId, out var assetId);
        return new LoaderContext(assetId);
    }

    // public void EnsureListCapacity<T>(int capacity) where T : AssetObject =>_store!.GetAssetList<T>().EnsureCapacity(capacity);

    [MethodImpl(MethodImplOptions.NoInlining)]
    public void ActivateFullLoader(AssetStore store, AssetGfxUploader uploader, Queue<AssetRecord>[] recordQueue)
    {
        InvalidOpThrower.ThrowIf(IsActive);

        _recordQueue = recordQueue;

        _store = store;
        _gfxUploader = uploader;

        _loaders[AssetKindUtils.ToAssetIndex<Shader>()] = new ShaderLoader(uploader);
        _loaders[AssetKindUtils.ToAssetIndex<Texture>()] = new TextureLoader(uploader);
        _loaders[AssetKindUtils.ToAssetIndex<Material>()] = new MaterialLoader(store, uploader);
        _loaders[AssetKindUtils.ToAssetIndex<Model>()] = new ModelLoader(uploader);


        foreach (var loader in _loaders)
            loader!.Setup();

        IsActive = true;

        Logger.LogString(LogScope.Assets, "Startup Asset Loader - Activated");
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    public void ActivateLazyLoader( AssetStore store, AssetGfxUploader gfx)
    {
        IsActive = true;
        _store ??= store;
        _gfxUploader ??= gfx;
        Logger.LogString(LogScope.Assets, "Asset Loader - Activated");

    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    public void DeactivateLoader()
    {
        foreach (var loader in _loaders)
            loader?.Teardown();

        for (var i = 0; i < _loaders.Length; i++)
            _loaders[i] = null!;

        _gfxUploader = null;
        IsActive = false;

        Logger.LogString(LogScope.Assets, "Asset Loader - Closed");
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    public bool ProcessLoader()
    {
        if (_recordQueue is null)
            throw new InvalidOperationException("Asset Queue is null");

        switch (_step)
        {
            case ProcessStepOrder.NotStarted: _step = ProcessStepOrder.Shaders; break;
            case ProcessStepOrder.Shaders:
                LoadShaders(_recordQueue[AssetKindUtils.ToAssetIndex(AssetKind.Shader)]);
                break;
            case ProcessStepOrder.Textures:
                LoadTextures(_recordQueue[AssetKindUtils.ToAssetIndex(AssetKind.Texture)]);
                break;
            case ProcessStepOrder.Meshes:
                LoadModel(_recordQueue[AssetKindUtils.ToAssetIndex(AssetKind.Model)]);
                break;
            case ProcessStepOrder.Materials:
                LoadMaterial(_recordQueue[AssetKindUtils.ToAssetIndex(AssetKind.Material)]);
                break;
            default:
                throw new ArgumentOutOfRangeException();
        }

        return _step == ProcessStepOrder.Finished;
    }

    private void Load<TAsset, TRecord>(AssetTypeLoader<TAsset, TRecord> loader, TRecord record, string path)
        where TAsset : AssetObject where TRecord : AssetRecord
    {
        var ctx = MakeContext(record, path);
        var asset = loader.LoadAsset(record, ctx);
        _store!.AddAsset(asset);

        if (loader.EmbeddedAssets.Count > 0)
            ProcessEmbedded(asset.Id, loader.EmbeddedAssets);
    }

    public void LoadShaders(Queue<AssetRecord> queue)
    {
        var loader = GetLoader<ShaderLoader>(AssetKind.Shader);
        while (queue.TryDequeue(out var record))
            Load(loader, (ShaderRecord)record, EnginePath.ShaderPath);

        _step = ProcessStepOrder.Textures;
    }

    public void LoadTextures(Queue<AssetRecord> queue)
    {
        int n = 6;

        var loader = GetLoader<TextureLoader>(AssetKind.Texture);
        while (n-- >= 0 && queue.TryDequeue(out var record))
            Load(loader, (TextureRecord)record, EnginePath.TexturePath);

        if (queue.Count == 0) _step = ProcessStepOrder.Meshes;
    }

    public void LoadModel(Queue<AssetRecord> queue)
    {
        var loader = GetLoader<ModelLoader>(AssetKind.Model);

        int n = 6;
        while (n-- >= 0 && queue.TryDequeue(out var record))
        {
            Load(loader, (ModelRecord)record, EnginePath.ModelPath);
        }

        if (queue.Count == 0) _step = ProcessStepOrder.Materials;
    }

    public void LoadMaterial(Queue<AssetRecord> queue)
    {
        var loader = GetLoader<MaterialLoader>(AssetKind.Material);
        while (queue.TryDequeue(out var record))
            Load(loader, (MaterialRecord)record, EnginePath.MaterialPath);

        _step = ProcessStepOrder.Finished;
    }

    public void ReloadShader(Shader shader)
    {
        if (!IsActive) throw new InvalidOperationException(nameof(IsActive));
        if (_gfxUploader == null) throw new InvalidOperationException(nameof(_gfxUploader));

        _loaders[AssetKindUtils.ToAssetIndex<Shader>()] ??= new ShaderLoader(_gfxUploader);
        var loader = (ShaderLoader)_loaders[AssetKindUtils.ToAssetIndex<Shader>()]!;
        if(!loader.IsActive) loader.Setup();
        _store!.Reload(shader, loader.ReloadShader);
    }

    private void ProcessEmbedded(AssetId originalAssetId, List<IEmbeddedAsset> embedded)
    {
        if (_store == null) throw new InvalidOperationException(nameof(_store));

        foreach (var it in embedded)
        {
            var assetId = _store.RegisterEmbedded(originalAssetId, it);
            switch (it)
            {
                case EmbeddedSceneTexture tex:
                    var texture = GetLoader<TextureLoader>(AssetKind.Texture).LoadEmbedded(assetId, tex);
                    _store.AddAsset(texture);
                    break;
                case EmbeddedSceneMaterial mat:
                    var material = GetLoader<MaterialLoader>(AssetKind.Material).LoadEmbedded(assetId, mat);
                    _store.AddAsset(material);
                    break;
            }
        }

        embedded.Clear();
    }

    private TLoader GetLoader<TLoader>(AssetKind kind) where TLoader : class, IAssetTypeLoader
    {
        var loader = _loaders[AssetKindUtils.ToAssetIndex(kind)];
        if (loader is not TLoader tLoader)
            throw new InvalidOperationException($"Loader: {kind} is null or wrong type");

        return tLoader;
    }
}