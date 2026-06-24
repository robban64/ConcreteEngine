using ConcreteEngine.Core.Common;
using ConcreteEngine.Core.Diagnostics.Logging;
using ConcreteEngine.Core.Engine;
using ConcreteEngine.Core.Engine.Assets;
using ConcreteEngine.Core.Engine.Assets.Descriptors;
using ConcreteEngine.Core.Engine.Assets.Utils;
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

    private AssetLoaderContext? _loaderContext;

    public AssetLoader(AssetManager assetManager, GfxContext gfx)
    {
        _assetManager = assetManager;
        _store = assetManager.Store;

        _loaders = new IAssetTypeLoader[AssetKindUtils.AssetTypeCount];

        _loaders[AssetKind.Shader.ToIndex()] = _shaderLoader = new ShaderLoader(gfx.Shaders);
        _loaders[AssetKind.Texture.ToIndex()] = _textureLoader = new TextureLoader(gfx.Textures);
        _loaders[AssetKind.Model.ToIndex()] = _modelLoader = new ModelLoader(_textureLoader, gfx.Meshes);
        _loaders[AssetKind.Material.ToIndex()] = _materialLoader = new MaterialLoader();
    }

    public bool IsActive => _loaderContext != null;

    public AssetLoaderContext GetLoaderContext()
    {
        if (_loaderContext is null) Throwers.InvalidOperation("Loader context is null");
        return _loaderContext;
    }

    private ImportContext MakeContext(AssetRecord record)
    {
        if (!_store.TryGetIdByGuid(record.Id, out var assetId))
            Throwers.NotFound(nameof(record.Id), $"AssetRecord '{record.Name}'");

        return new ImportContext(assetId, _assetManager);
    }

    public AssetLoaderContext ActivateFullLoader()
    {
        if (IsActive || _loaderContext != null) Throwers.InvalidOperation("Invalid states");
        _loaderContext = new AssetLoaderContext(true);
        foreach (var loader in _loaders) loader.Activate(true);
        Logger.LogString(LogScope.Assets, "Startup Asset Loader - Activated");
        return _loaderContext;
    }

    public void ReactiveLoader(AssetKind kind)
    {
        if (_loaders[kind.ToIndex()].IsActive) return;

        _loaderContext ??= new AssetLoaderContext(false);

        if (kind == AssetKind.Model)
        {
            ReactiveLoader(AssetKind.Texture);
            ReactiveLoader(AssetKind.Material);
        }

        _loaders[kind.ToIndex()].Activate(false);

        Logger.LogString(LogScope.Assets, $"Loader ({EnumCache<AssetKind>.Names[(int)kind]}) - Reactivated");
    }

    public void DeactivateLoader()
    {
        _loaderContext = null;
        foreach (var loader in _loaders) loader.DeActivate();

        Logger.LogString(LogScope.Assets, "Asset Loader - Closed");
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

    public bool ProcessLoader()
    {
        if (_loaderContext is null) Throwers.InvalidOperation("Loader context is null");
        if (_loaderContext.TotalQueued == 0) return true;

        if (_step == ProcessStepOrder.NotStarted) _step++;

        bool done = _step switch
        {
            ProcessStepOrder.Shaders => LoadShaders(),
            ProcessStepOrder.Textures => LoadTextures(),
            ProcessStepOrder.Meshes => LoadModels(),
            ProcessStepOrder.Materials => LoadMaterial(),
            ProcessStepOrder.Finished => true,
            _ => throw new ArgumentOutOfRangeException()
        };

        if (done && _step != ProcessStepOrder.Finished) _step++;
        return _step == ProcessStepOrder.Finished;
    }

    private void ProcessEmbedded(Model model, List<IEmbeddedAsset> embedded)
    {
        int i;
        for (i = 0; i < embedded.Count; i++)
        {
            if (embedded[i] is not EmbeddedSceneTexture tex) break;
            var assetId = _assetManager.RegisterEmbedded(model.Id, tex);
            var texture = _textureLoader.LoadEmbedded(assetId, tex);
            _store.AddAsset(texture);
            model.SetTexture(tex.TextureIndex, texture);
        }

        for (; i < embedded.Count; i++)
        {
            if (embedded[i] is not EmbeddedSceneMaterial mat) continue;
            var assetId = _assetManager.RegisterEmbedded(model.Id, mat);
            var material = _materialLoader.LoadEmbedded(assetId, mat);
            _store.AddAsset(material);
            model.SetMaterial(mat.MaterialIndex, material);
        }

        if (_textureLoader.StoredEmbeddedCount > 0)
            Throwers.InvalidOperation("Texture loader has stored embedded assets");

        embedded.Clear();
    }

    private TAsset Load<TAsset, TRecord>(AssetTypeLoader<TAsset, TRecord> loader, TRecord record)
        where TAsset : AssetObject where TRecord : AssetRecord
    {
        var asset = loader.LoadAsset(record, MakeContext(record));
        _store.AddAsset(asset);
        return asset;
    }

    private bool LoadShaders()
    {
        _shaderLoader.ImportAllShaders(_loaderContext!.GetQueue(AssetKind.Shader));
        bool done = _loaderContext.DrainQueue<ShaderRecord>(AssetKind.Shader, 0,
            record => Load(_shaderLoader, record)
        );

        if (done) _assetManager.AttachShaders();
        return done;
    }

    private bool LoadTextures()
    {
        return _loaderContext!.DrainQueue<TextureRecord>(AssetKind.Texture, 8,
            record => Load(_textureLoader, record)
        );
    }

    private bool LoadModels()
    {
        return _loaderContext!.DrainQueue<ModelRecord>(AssetKind.Model, 8,
            record =>
            {
                var model = Load(_modelLoader, record);
                ProcessEmbedded(model, _modelLoader.EmbeddedAssets);
            });
    }

    private bool LoadMaterial()
    {
        return _loaderContext!.DrainQueue<MaterialRecord>(AssetKind.Material, 0,
            record => Load(_materialLoader, record)
        );
    }

/*
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private TLoader GetLoader<TLoader>() where TLoader : class, IAssetTypeLoader
        => (TLoader)_loaders[TLoader.Kind.ToIndex()];
*/

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