using System.Runtime.CompilerServices;
using ConcreteEngine.Core.Common;
using ConcreteEngine.Core.Diagnostics.Logging;
using ConcreteEngine.Core.Engine;
using ConcreteEngine.Core.Engine.Assets;
using ConcreteEngine.Core.Engine.Assets.Descriptors;
using ConcreteEngine.Core.Engine.Assets.Utils;
using ConcreteEngine.Core.Engine.Configuration;
using ConcreteEngine.Engine.Assets.Loader;
using ConcreteEngine.Graphics;

namespace ConcreteEngine.Engine.Assets;

internal sealed class AssetLoader
{
    private ProcessStepOrder _step;

    private readonly AssetManager _assetManager;

    private readonly AssetStore _store;

    private readonly ShaderLoader _shaderLoader;
    private readonly TextureLoader _textureLoader;
    private readonly ModelLoader _modelLoader;
    private readonly MaterialLoader _materialLoader;

    private readonly IAssetTypeLoader[] _loaders;

    private readonly Queue<AssetRecord>[] _recordQueue;

    public AssetLoader(AssetManager assetManager, GfxContext gfx)
    {
        _assetManager = assetManager;   
        _store = assetManager.Store;
        
        _loaders = new IAssetTypeLoader[AssetKindUtils.AssetTypeCount];

        _loaders[AssetKind.Shader.ToIndex()] = _shaderLoader = new ShaderLoader(gfx.Shaders);
        _loaders[AssetKind.Texture.ToIndex()] =_textureLoader = new TextureLoader(gfx.Textures);
        _loaders[AssetKind.Model.ToIndex()] =_modelLoader = new ModelLoader(_textureLoader, gfx.Meshes);
        _loaders[AssetKind.Material.ToIndex()] =_materialLoader = new MaterialLoader();
        
        _recordQueue = new Queue<AssetRecord>[AssetKindUtils.AssetTypeCount];
    }

    public Queue<AssetRecord>[] GetQueues() => _recordQueue;
    
    public bool IsActive => _shaderLoader.IsActive || _textureLoader.IsActive || _modelLoader.IsActive || _materialLoader.IsActive;

    private LoaderContext MakeContext(AssetRecord record, bool isHotReload = false)
    {
        if (!_store.TryGetIdByGuid(record.GId, out var assetId))
            Throwers.NotFound(nameof(record.GId), $"AssetRecord '{record.Name}'");
        
        return new LoaderContext(assetId, _assetManager);
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    public void ActivateFullLoader()
    {
        if(IsActive) Throwers.InvalidOperation(nameof(IsActive));

        foreach (var loader in _loaders)
            loader.Activate(true);

        Logger.LogString(LogScope.Assets, "Startup Asset Loader - Activated");
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    public void ReactiveLoader(AssetKind kind)
    {
        if(_loaders[kind.ToIndex()].IsActive) return;
        
        if (kind == AssetKind.Model)
        {
            ReactiveLoader(AssetKind.Texture);
            ReactiveLoader(AssetKind.Material);
        }
        
        _loaders[kind.ToIndex()].Activate(false);
        
        var assetKindName = EnumCache<AssetKind>.Names[(int)kind];
        Logger.LogString(LogScope.Assets, $"Loader ({assetKindName}) - Reactivated");
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    public void DeactivateLoader()
    {
        foreach (var loader in _loaders)
            loader.DeActivate();

        Logger.LogString(LogScope.Assets, "Asset Loader - Closed");
    }

    public bool ProcessLoader(out AssetKind finishedKind)
    {
        if (_recordQueue.Length == 0)
            Throwers.InvalidOperation("Asset Queue is empty");

        finishedKind = AssetKind.Unknown;
        switch (_step)
        {
            case ProcessStepOrder.NotStarted: _step = ProcessStepOrder.Shaders; break;
            case ProcessStepOrder.Shaders:
                LoadShaders(_recordQueue[AssetKind.Shader.ToIndex()]);
                finishedKind = AssetKind.Shader;
                break;
            case ProcessStepOrder.Textures:
                LoadTextures(_recordQueue[AssetKind.Texture.ToIndex()]);
                if(_step != ProcessStepOrder.Textures) finishedKind = AssetKind.Texture;
                break;
            case ProcessStepOrder.Meshes:
                LoadModel(_recordQueue[AssetKind.Model.ToIndex()]);
                if(_step != ProcessStepOrder.Meshes) finishedKind = AssetKind.Model;
                break;
            case ProcessStepOrder.Materials:
                LoadMaterial(_recordQueue[AssetKind.Material.ToIndex()]);
                finishedKind = AssetKind.Material;
                break;
            default:
                return Throwers.Unreachable<bool>(nameof(_step));
        }

        return _step == ProcessStepOrder.Finished;
    }
    
    private void ProcessEmbedded(Model model, List<IEmbeddedAsset> embedded)
    {
        var hasTexture = false;
        foreach (var it in embedded)
        {
            var assetId = _assetManager.RegisterEmbedded(model.Id, it);
            switch (it)
            {
                case EmbeddedSceneTexture tex:
                    hasTexture = true;
                    var texture = _textureLoader.LoadEmbedded(assetId, tex);
                    _store.AddAsset(texture);
                    model.SetTexture(tex.TextureIndex, texture);
                    break;
                case EmbeddedSceneMaterial mat:
                    var material = _materialLoader.LoadEmbedded(assetId, mat);
                    _store.AddAsset(material);
                    model.SetMaterial(mat.MaterialIndex, material);
                    break;
            }
        }

        if (hasTexture && _textureLoader.StoredEmbeddedCount > 0)
            Throwers.InvalidOperation("Texture loader has stored embedded assets");

        embedded.Clear();
    }


    private void Load<TAsset, TRecord>(AssetTypeLoader<TAsset, TRecord> loader, TRecord record)
        where TAsset : AssetObject where TRecord : AssetRecord
    {
        var ctx = MakeContext(record);
        var asset = loader.LoadAsset(record, ctx);
        _store.AddAsset(asset);

        if (loader is ModelLoader modelLoader && asset is Model model)
            ProcessEmbedded(model, modelLoader.EmbeddedAssets);
    }

    public void LoadShaders(Queue<AssetRecord> queue)
    {
        var loader = GetLoader<ShaderLoader>();
        loader.LoadAllShaders(queue);
        while (queue.TryDequeue(out var record))
            Load(loader, (ShaderRecord)record);

        _step = ProcessStepOrder.Textures;
    }

    public void LoadTextures(Queue<AssetRecord> queue)
    {
        int n = 8;
        while (n-- >= 0 && queue.TryDequeue(out var record))
            Load(_textureLoader, (TextureRecord)record);

        if (queue.Count == 0) _step = ProcessStepOrder.Meshes;
    }

    public void LoadModel(Queue<AssetRecord> queue)
    {
        int n = 8;
        while (n-- >= 0 && queue.TryDequeue(out var record))
            Load(_modelLoader, (ModelRecord)record);

        if (queue.Count == 0) _step = ProcessStepOrder.Materials;
    }

    public void LoadMaterial(Queue<AssetRecord> queue)
    {
        while (queue.TryDequeue(out var record))
            Load(_materialLoader, (MaterialRecord)record);

        _step = ProcessStepOrder.Finished;
    }

    public void Reload<TAsset>(TAsset asset) where TAsset : AssetObject
    {
        ArgumentNullException.ThrowIfNull(asset);
        
        _store.TryGetFileBindings(asset.Id, out var fileIds);
        var files = new AssetFile[fileIds.Length];
        for (var i = 0; i < fileIds.Length; i++)
            files[i] = _assetManager.Files.Get(fileIds[i]);

        var loader = _loaders[asset.Kind.ToIndex()];
        if (loader is not IAssetTypeLoader<TAsset> tLoader)
        {
            Throwers.InvalidArgument(nameof(asset.Kind), $"Loader {typeof(TAsset).Name} is null");
            return;
        }

        if (!tLoader.IsActive)
           ReactiveLoader(asset.Kind);

        tLoader.Reload(asset, files);

        if (files.Length > 0) _assetManager.RegisterExistingBindings(asset.Id, files);

    }
    
    private TLoader GetLoader<TLoader>() where TLoader : class, IAssetTypeLoader
    {
        var loader = _loaders[TLoader.Kind.ToIndex()];
        if (loader is TLoader tLoader) return tLoader;

        Throwers.InvalidArgument($"Loader: {TLoader.Kind} is null or wrong type");
        return null;
    }

     private enum ProcessStepOrder
    {
        NotStarted,
        Shaders,
        Textures,
        Meshes,
        Materials,
        Finished
    }
}