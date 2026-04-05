using System.Runtime.CompilerServices;
using ConcreteEngine.Core.Common;
using ConcreteEngine.Core.Diagnostics.Logging;
using ConcreteEngine.Core.Diagnostics.Time;
using ConcreteEngine.Core.Engine.Assets;
using ConcreteEngine.Core.Engine.Configuration;
using ConcreteEngine.Engine.Assets.Descriptors;
using ConcreteEngine.Engine.Assets.Loader.Data;
using ConcreteEngine.Engine.Assets.Utils;
using ConcreteEngine.Engine.Configuration.IO;
using ConcreteEngine.Engine.Editor.Diagnostics;
using ConcreteEngine.Graphics.Gfx.Handles;

namespace ConcreteEngine.Engine.Assets.Loader;

internal sealed class AssetLoader(AssetStore store, AssetGfxUploader gfxUploader)
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
    public bool IsActive { get; private set; }
    private ProcessStepOrder _step;

    private readonly Queue<AssetRecord>[] _recordQueue = new Queue<AssetRecord>[AssetKindUtils.AssetTypeCount];
    private readonly IAssetTypeLoader?[] _loaders = new IAssetTypeLoader[AssetKindUtils.AssetTypeCount];

    public Queue<AssetRecord>[] GetQueues() => _recordQueue;

    private LoaderContext MakeContext(AssetRecord record, string path, bool isHotReload = false)
    {
        if (!store.TryGetIdByGuid(record.GId, out var asset))
            throw new InvalidOperationException($"AssetRecord '{record.Name}' no registered Guid");
        return new LoaderContext(asset, store);
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    public void ActivateFullLoader()
    {
        InvalidOpThrower.ThrowIf(IsActive);

        _loaders[AssetKind.Shader.ToIndex()] = new ShaderLoader(gfxUploader);
        _loaders[AssetKind.Texture.ToIndex()] = new TextureLoader(gfxUploader);
        _loaders[AssetKind.Material.ToIndex()] = new MaterialLoader(store, gfxUploader);
        _loaders[AssetKind.Model.ToIndex()] = new ModelLoader(gfxUploader);


        foreach (var loader in _loaders)
            loader!.Setup(true);

        IsActive = true;

        Logger.LogString(LogScope.Assets, "Startup Asset Loader - Activated");
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    public void ActivateLazyLoader()
    {
        IsActive = true;
        Logger.LogString(LogScope.Assets, "Asset Loader - Activated");
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    public void DeactivateLoader()
    {
        foreach (var loader in _loaders)
            loader?.Teardown();

        for (var i = 0; i < _loaders.Length; i++)
            _loaders[i] = null;

        IsActive = false;

        Logger.LogString(LogScope.Assets, "Asset Loader - Closed");
    }

    private static AvgFrameTimer avg;
    public bool ProcessLoader()
    {
        if (_recordQueue.Length == 0)
            throw new InvalidOperationException("Asset Queue is empty");

        switch (_step)
        {
            case ProcessStepOrder.NotStarted: _step = ProcessStepOrder.Shaders; break;
            case ProcessStepOrder.Shaders:
                LoadShaders(_recordQueue[AssetKind.Shader.ToIndex()]);
                break;
            case ProcessStepOrder.Textures:
                avg.BeginSample();
                LoadTextures(_recordQueue[AssetKind.Texture.ToIndex()]);
                avg.EndSample();
                avg.ResetAndPrint("Textures");
                break;
            case ProcessStepOrder.Meshes:
                LoadModel(_recordQueue[AssetKind.Model.ToIndex()]);
                break;
            case ProcessStepOrder.Materials:
                LoadMaterial(_recordQueue[AssetKind.Material.ToIndex()]);
                break;
            default:
                throw new ArgumentOutOfRangeException();
        }

        return _step == ProcessStepOrder.Finished;
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private void Load<TAsset, TRecord>(AssetTypeLoader<TAsset, TRecord> loader, TRecord record, string path)
        where TAsset : AssetObject where TRecord : AssetRecord
    {
        var ctx = MakeContext(record, path);
        var asset = loader.LoadAsset(record, ctx);
        store.AddAsset(asset);

        if (loader.EmbeddedAssets.Count > 0)
            ProcessEmbedded(asset.Id, loader.EmbeddedAssets);
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    public void LoadShaders(Queue<AssetRecord> queue)
    {
        var loader = GetLoader<ShaderLoader>(AssetKind.Shader);
        loader.LoadAllShaders(queue);
        while (queue.TryDequeue(out var record))
            Load(loader, (ShaderRecord)record, EnginePath.ShaderPath);

        _step = ProcessStepOrder.Textures;
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    public void LoadTextures(Queue<AssetRecord> queue)
    {
        int n = 6;

        var loader = GetLoader<TextureLoader>(AssetKind.Texture);
        while (n-- >= 0 && queue.TryDequeue(out var record))
            Load(loader, (TextureRecord)record, EnginePath.TexturePath);

        if (queue.Count == 0) _step = ProcessStepOrder.Meshes;
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
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

    [MethodImpl(MethodImplOptions.NoInlining)]
    public void LoadMaterial(Queue<AssetRecord> queue)
    {
        var loader = GetLoader<MaterialLoader>(AssetKind.Material);
        while (queue.TryDequeue(out var record))
            Load(loader, (MaterialRecord)record, EnginePath.MaterialPath);

        _step = ProcessStepOrder.Finished;
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    public void ReloadShader(AssetId shaderId)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(shaderId.Value,nameof(shaderId));
        if (!IsActive) throw new InvalidOperationException(nameof(IsActive));
        var shader = store.Get<Shader>(shaderId);
        
        var index = AssetKind.Shader.ToIndex();
        _loaders[index] ??= new ShaderLoader(gfxUploader);
        var loader = (ShaderLoader)_loaders[index]!;

        if (!loader.IsActive) loader.Setup(false);
        store.Reload(shader, loader.ReloadShader);
    }

    private void ProcessEmbedded(AssetId originalAssetId, List<IEmbeddedAsset> embedded)
    {
        foreach (var it in embedded)
        {
            var assetId = store.RegisterEmbedded(originalAssetId, it);
            switch (it)
            {
                case EmbeddedSceneTexture tex:
                    var texture = GetLoader<TextureLoader>(AssetKind.Texture).LoadEmbedded(assetId, tex);
                    store.AddAsset(texture);
                    break;
                case EmbeddedSceneMaterial mat:
                    var material = GetLoader<MaterialLoader>(AssetKind.Material).LoadEmbedded(assetId, mat);
                    store.AddAsset(material);
                    break;
            }
        }

        embedded.Clear();
    }

    private TLoader GetLoader<TLoader>(AssetKind kind) where TLoader : class, IAssetTypeLoader
    {
        var loader = _loaders[kind.ToIndex()];
        if (loader is not TLoader tLoader)
            throw new InvalidOperationException($"Loader: {kind} is null or wrong type");

        return tLoader;
    }
}