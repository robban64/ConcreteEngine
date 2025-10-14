#region

using ConcreteEngine.Common;
using ConcreteEngine.Core.Assets.Config;
using ConcreteEngine.Core.Assets.Data;
using ConcreteEngine.Core.Assets.Materials;
using ConcreteEngine.Core.Engine;
using ConcreteEngine.Graphics.Gfx;

#endregion

namespace ConcreteEngine.Core.Assets;

public interface IAssetSystem : IGameEngineSystem
{
    IAssetStore Store { get; }
}

public sealed class AssetSystem : IAssetSystem
{
    private static bool _initialized = false;

    private readonly string _assetPath;
    private readonly string _manifestFilename;
    private AssetManifest _manifest = null!;

    private MaterialStore _materialStore = null!;
    public MaterialStore MaterialStore => _materialStore;

    public bool IsLoading { get; private set; } = false;

    private string BasePath => Path.Combine(Directory.GetCurrentDirectory(), _assetPath);


    private readonly AssetStore _assetStore = new();

    private AssetConfigLoader? _configLoader;
    private AssetProcessor? _processor;
    private AssetGfxUploader? _uploader;
    private AssetLoader? _loader;

    internal AssetStore AssetStore => _assetStore;

    public IAssetStore Store => _assetStore;


    internal AssetSystem(
        string assetPath = "assets",
        string manifestFilename = "manifest.json")
    {
        _assetPath = assetPath;
        _manifestFilename = manifestFilename;

        AssetPaths.AssetFolder = _assetPath;
    }

    internal void Initialize()
    {
        if (_initialized)
            throw new InvalidOperationException("AssetSystem already initialized");

        _configLoader = new AssetConfigLoader(_assetPath, _manifestFilename);
        _manifest = _configLoader.LoadAssetManifest();

        _initialized = true;
    }


    internal void StartLoader(GfxContext gfx)
    {
        ArgumentNullException.ThrowIfNull(gfx, nameof(gfx));
        ArgumentNullException.ThrowIfNull(_configLoader, nameof(_configLoader));

        InvalidOpThrower.ThrowIf(IsLoading, nameof(IsLoading));

        IsLoading = true;

        _uploader = new AssetGfxUploader(gfx);
        _loader = new AssetLoader();
        _processor = new AssetProcessor(_loader, _configLoader, _manifest);
        _processor.Start(_assetStore, _uploader);
    }

    internal bool ProcessLoader(int n)
    {
        if (_loader == null)
            throw new InvalidOperationException("Asset loader is not initialized");

        for (var i = 0; i < n; i++)
        {
            if (_processor!.Process()) return true;
        }

        return false;
    }

    internal MaterialStore FinishLoading()
    {
        var materialLoader = new MaterialLoader();
        var materials = materialLoader.LoadMaterials(_assetStore, _manifest.ResourceLayout, _configLoader!);
        _materialStore = new MaterialStore(materials!);

        _processor?.Finish();
        _processor = null;
        IsLoading = true;
        return _materialStore;
    }

    public void Shutdown()
    {
        /*
        foreach (var asset in _store.Values)
            if (asset is IDisposable disposable)
                disposable.Dispose();
                */
    }
}